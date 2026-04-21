using Puma.MDE.Data;
using Puma.MDE.SwapUtils;
using System.Collections.Generic;


namespace Puma.MDE.OPUS.OrderImport
{
    public interface ISwapOrderImportGateway
    {
        bool ImportOrder(SwapRawOrder rawOrder, out SwapOrder importedOrder, out List<OrderManager.Error> errors);
    }
}