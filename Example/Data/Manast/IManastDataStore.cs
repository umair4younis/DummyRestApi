using Puma.MDE.SwapUtils;
using System;
using System.Collections.Generic;

namespace Puma.MDE.Data.Manast
{
    internal interface IManastDataStore
    {
        int AccountActivePortfolioRowsCount(int accountId);
        int AccountOrderRowsCount(object id);
        ManastOrderDTO AddOrderRow(ManastAccountDTO account, string description, byte type, double indexValue, DateTime tradeDate, DateTime valueDate, DateTime today, int v, double certificates, object value, int updatePoolUpon);
        void AddPoolRow(ManastInstrumentDTO ir, double nominal, int v);
        ManastTradeDTO AddTradeRow(ManastOrderDTO order, ManastInstrumentDTO ir, object dbName, string description, double v1, double price, double accrued, double totalFee, string currency, double fXRate, double targetWeight, int v2, bool pool);
        bool DeleteOrder(ManastOrderDTO order);
        object GetAccountOrderRows(object id);
        IEnumerable<ManastAccountPortfolioDTO> GetAccountPortfolioRows(object id, object m_IncludeClosedPositions);
        IEnumerable<ManastAccountPortfolioDTO> GetAccountPortfolioRows(int id, object m_IncludeClosedPositions);
        ManastInstrumentDTO GetInstrumentByName(string v);
        ManastInstrumentDTO GetInstrumentByRICorISIN(string empty, string iSIN);
        object GetOrderAccountHistoryRows(ManastOrderDTO order);
        SwapPool GetPoolRowByInstrumentId(object id);
        object GetPriceRuleRowsByAccountId(object id);
        ManastAccountDTO GetSwapAccountByName(string accountName);
        object GetSwapAccountPortfolioByAccountIDInstrumentID(int id1, object id2, bool v);
        void RemoveRow(ManastAccountHistoryDTO row);
    }
}