using Puma.MDE.Data.Manast;
using System;
using System.Collections.Generic;

namespace Puma.MDE.Data.Manast
{
    internal class ManastAccountDTOHelper
    {
        internal IEnumerable<ManastAccountPortfolioDTO> AccountPortfolioRows(ManastAccountDTO account)
        {
            throw new NotImplementedException();
        }

        internal IEnumerable<ManastEventDTO> GetSortedEventRows(ManastAccountDTO account, bool v)
        {
            throw new NotImplementedException();
        }

        internal List<ManastOrderDTO> GetSortedOrderRows(ManastAccountDTO ar, bool v1, bool v2)
        {
            throw new NotImplementedException();
        }

        internal ManastAccountPortfolioDTO GetSwapAccountPortfolioByInstrumentId(int id1, object id2)
        {
            throw new NotImplementedException();
        }

        internal ManastAccountPortfolioDTO GetSwapAccountPortfolioByInstrumentId(object accountId, object value)
        {
            throw new NotImplementedException();
        }

        internal IEnumerable<ManastOrderDTO> OrderRows(ManastAccountDTO account)
        {
            throw new NotImplementedException();
        }

        internal int OrderRowsCount(ManastAccountDTO ar)
        {
            throw new NotImplementedException();
        }
    }
}