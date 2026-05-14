using Puma.MDE.Data;
using Puma.MDE.Data.Manast;
using Puma.MDE.SwapUtils;
using System.Collections.Generic;


namespace Puma.MDE.OPUS.OrderImport
{
    public class OrderManagerImportGateway : ISwapOrderImportGateway
    {
        public bool ImportOrder(SwapRawOrder rawOrder, out ManastOrderDTO importedOrder, out List<OrderManager.Error> errors)
        {
            return OrderManager.ImportOrder(rawOrder, out importedOrder, out errors);
        }
    }
}