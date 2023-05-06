using System.IO;
using Hakoniwa.PluggableAsset;
using Hakoniwa.PluggableAsset.Assets;
using UnityEngine;
using UnityEngine.UI;

public class LoadFromFileExample : MonoBehaviour
{

    private void Start()
    {
        string configPath = "./core_config.json";
        AssetConfigLoader.Load(configPath);

        Debug.Log("childcount=" + this.transform.childCount);
        for (int i = 0; i < this.transform.childCount; i++)
        {
            Transform child = this.transform.GetChild(i);
            Debug.Log(child.name);
            var ctrl = child.GetComponentInChildren<IInsideAssetController>();
            ctrl.Initialize();
            AssetConfigLoader.AddInsideAsset(ctrl);
        }
#if false
        AssetBundle assetBundle = AssetBundle.LoadFromFile("Assets/AssetBundles/hakoniwa/assets/samplerobo");
        if (assetBundle == null)
        {
            Debug.Log("Failed to load AssetBundle!");
            return;
        }

        GameObject prefab = assetBundle.LoadAsset<GameObject>("SampleRobo");
        if (prefab == null)
        {
            Debug.Log("Failed to load LoadAsset!");
            return;
        }

        GameObject instance = Instantiate(prefab);

        assetBundle.Unload(false);
#endif
    }
    private void FixedUpdate()
    {
        //ReadPdu();
        ExecuteSimulation();
        //WritePdu();
    }

    public void ReadPdu()
    {
        /********************
         * Inside assets
         * - Recv Actuation Data
        ********************/
        foreach (var connector in AssetConfigLoader.RefPduChannelConnector())
        {
            if (
                ((connector.GetName() == null) || connector.GetName().Equals("None"))
                    && (connector.Reader != null)
                )
            {
                connector.Reader.Recv();
            }
        }
    }

    public void WritePdu()
    {
        /********************
         * Onside assets 
         * - Send Sensor Data 
         ********************/
        foreach (var connector in AssetConfigLoader.RefPduChannelConnector())
        {
            if (
                ((connector.GetName() == null) || connector.GetName().Equals("None"))
                    && (connector.Writer != null))
            {
                connector.Writer.SendWriterPdu();
                connector.Writer.SendReaderPdu();
            }
        }
    }

    public void ExecuteSimulation()
    {
        /********************
         * Inside Assets 
         * - Do Simulation
         ********************/
        foreach (var asset in AssetConfigLoader.GetInsideAssets())
        {
            asset.DoActuation();
        }


        foreach (var asset in AssetConfigLoader.GetInsideAssets())
        {
            asset.CopySensingDataToPdu();
        }
    }
}