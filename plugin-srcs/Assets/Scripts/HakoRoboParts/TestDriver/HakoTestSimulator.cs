using System.IO;
using Hakoniwa.PluggableAsset;
using Hakoniwa.PluggableAsset.Assets;
using UnityEngine;
using UnityEngine.UI;

namespace Hakoniwa.PluggableAsset.Assets.Robot.Parts.TestDriver
{
    public class HakoTestSimulator : MonoBehaviour
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
}
