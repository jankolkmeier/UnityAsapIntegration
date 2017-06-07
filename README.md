# UnityAsapIntegration

## How to setup
1. Open unity project / create blank project
    - w/ Unity (5.6.x)

2. Import UMA 2 from asset store (v2.5.x)
    - https://www.assetstore.unity3d.com/en/#!/content/35611

3. in the Assets folder, download or git clone this project:
    - https://github.com/jankolkmeier/UnityAsapIntegration

4. One useful Unity project setting is...
    - Edit -> Project Settings -> Player -> (In inspector window:) "Run in Background": enabled
    - If not enabled, unity will not run when another window is focussed (i.e. the Asap window).

5. Make sure an Apollo broker is running. 
    - https://activemq.apache.org/apollo/

6. Open the example scene in unity: UnityAsapIntegration/ExampleScenes/AsapExampleUMA
    - Press Play

7. Load Asap with the Unity/agentspecs/uma_default.xml agentspec from HMIUnityResources
    - Once the second empty java window pops up, everything should be working.


## How to use
TODO
