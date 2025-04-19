using UnityEngine;

namespace LMCore.IO
{
    public class CustomBindingConf : AbsBindingConf
    {
        [SerializeField]
        string storageLocation;

        public override GameSettings.StringSetting Settings =>
            string.IsNullOrEmpty(storageLocation) ? null : GameSettings.GetCustomString(storageLocation);

        public bool Defines(string id, int bindingIndex) =>
            storageLocation == id && this.bindingIndex == bindingIndex;
    }
}
