using System.Runtime.CompilerServices;
using System.Text;

namespace MqManager.Core.Services;

internal static class ModuleInitializers
{
    // IBM MQ messages carry whatever CCSID the originating app used — often an
    // EBCDIC or DBCS (e.g. GBK/Big5) codepage for messages from external/mainframe
    // producers, as opposed to UTF-8 (1208) used by this tool's own PutMessage.
    // .NET only ships Unicode encodings by default, so MQMessage.ReadString throws
    // NotSupportedException for those CCSIDs unless this provider is registered.
    [ModuleInitializer]
    internal static void RegisterCodePages()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }
}
