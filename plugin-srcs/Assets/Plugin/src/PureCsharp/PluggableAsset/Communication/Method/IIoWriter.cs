using Hakoniwa.PluggableAsset.Communication.Pdu;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hakoniwa.PluggableAsset.Communication.Method
{
    public interface IIoWriter
    {
        string GetName();
        void Initialize(IIoWriterConfig config);
        void Flush(IPduCommData data);
    }
}
