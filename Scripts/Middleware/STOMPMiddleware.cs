using Apache.NMS;
using Apache.NMS.Util;
using Apache.NMS.Stomp;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class STOMPMiddleware : IMiddleware {

    public string topicRead;
    public string topicWrite;
    public string address;
    private string user;
    private string pass;

    bool networkOpen;
    ISession session;
    IConnectionFactory factory;
    IConnection connection;
    Thread apolloWriterThread;
    Thread apolloReaderThread;

    AutoResetEvent semaphore = new AutoResetEvent(false);
    System.TimeSpan receiveTimeout = System.TimeSpan.FromMilliseconds(250);

    Queue<string> readMessageQueue;
    private object _readMessageQueueLock = new object();

    Queue<string> writeMessageQueue;
    private object _writeMessageQueueLock = new object();

    public STOMPMiddleware(string address, string topicRead, string topicWrite, string user, string pass) {
        this.topicRead = topicRead;
        this.topicWrite = topicWrite;
        this.address = address;
        this.user = user;
        this.pass = pass;

        readMessageQueue = new Queue<string>();
        writeMessageQueue = new Queue<string>();

        STOMPStart();
    }

    public void SendMessage(string msg) {
        lock (_writeMessageQueueLock) {
            writeMessageQueue.Enqueue(msg);
        }
    }

    public string ReadMessage() {
        if (readMessageQueue.Count > 0) {
            while (readMessageQueue.Count > 1) readMessageQueue.Dequeue();
            lock (_readMessageQueueLock) {
                return readMessageQueue.Dequeue();
            }
        } else {
            return "";
        }
    }

    public void Close() {
        networkOpen = false;
        if (apolloWriterThread != null && !apolloWriterThread.Join(500)) {
            Debug.LogWarning("Could not close apolloWriterThread");
            apolloWriterThread.Abort();
        }

        if (apolloReaderThread != null && !apolloReaderThread.Join(500)) {
            Debug.LogWarning("Could not close apolloReaderThread");
            apolloWriterThread.Abort();
        }
        if (connection != null) connection.Close();
    }

    void STOMPStart() {
        try {
            System.Uri connecturi = new System.Uri(address);
            Debug.Log("Apollo connecting to " + connecturi);
            factory = new NMSConnectionFactory(connecturi);
            // NOTE: ensure the nmsprovider-activemq.config file exists in the executable folder.
            connection = factory.CreateConnection(user, pass);
            session = connection.CreateSession();
            networkOpen = true;
            connection.Start();
        } catch (System.Exception e) {
            Debug.Log("Apollo Start Exception " + e);
        }

        apolloWriterThread = new Thread(new ThreadStart(ApolloWriter));
        apolloWriterThread.Start();

        apolloReaderThread = new Thread(new ThreadStart(ApolloReader));
        apolloReaderThread.Start();
    }

    void ApolloWriter() {
        try {
            IDestination destination_Write = SessionUtil.GetDestination(session, topicWrite);
            IMessageProducer producer = session.CreateProducer(destination_Write);
            producer.DeliveryMode = MsgDeliveryMode.NonPersistent;
            producer.RequestTimeout = receiveTimeout;
            while (networkOpen) {
                if (writeMessageQueue.Count > 0) {
                    string msg;
                    lock (_writeMessageQueueLock) {
                        msg = writeMessageQueue.Dequeue();
                    }
                    try {
                        if (msg.Length > 0) producer.Send(session.CreateTextMessage(msg));
                    } catch (RequestTimedOutException rte) {
                        Debug.Log("Timeout " + rte);
                        continue;
                    }
                }
            }
        } catch (System.Exception e) {
            Debug.Log("ApolloWriter Exception " + e);
        }
    }

    void ApolloReader() {
        try {
            IDestination destination_Read = SessionUtil.GetDestination(session, topicRead);
            IMessageConsumer consumer = session.CreateConsumer(destination_Read);
            consumer.Listener += new MessageListener(OnMessage);
            while (networkOpen) {
                semaphore.WaitOne((int) receiveTimeout.TotalMilliseconds, true);
            }
        } catch (System.Exception e) {
            Debug.Log("ApolloReader Exception " + e);
        }
    }

    void OnMessage(IMessage receivedMsg) {
        lock (_readMessageQueueLock) {
            readMessageQueue.Enqueue((receivedMsg as ITextMessage).Text);
        }
        semaphore.Set();
    }

}