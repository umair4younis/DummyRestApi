using Puma.MDE.Data;
using Puma.MDE.SwapUtils;
using System.Collections.Generic;


namespace Puma.MDE.OPUS.OrderImport
{
    public class OrderManagerImportGateway : ISwapOrderImportGateway
    {
        public bool ImportOrder(SwapRawOrder rawOrder, out SwapOrder importedOrder, out List<OrderManager.Error> errors)
        {
            return OrderManager.ImportOrder(rawOrder, out importedOrder, out errors);
        }
    }
}