using NLog;
using Puma.MDE.Data;
using Puma.MDE.Data.Manast;
using Puma.MDE.SwapAccountPricing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Puma.MDE.SwapUtils
{
    /// <summary>
    /// Imports and executes the orders in the local ADO.Net database
    /// </summary>
	public class OrderManager
    {
        private Logger logger = Engine.Instance.Log;

        public static object ManastAccountDTOHelper { get; private set; }
        public static object ManastOrderDTOHelper { get; private set; }
        public static object ManastTradeDTOHelper { get; private set; }
        public static object ServiceFactory { get; private set; }
        public static object DataAccess { get; private set; }

        /// <summary>
        /// Describes an error that occurred while importing the order
        /// </summary>
        public class Error
        {
            public TypeManastImportError Type;
            public string Message;
            public SwapRawOrder.TradeRow OrderRow;
            public bool RequiresUserAction;

            public Error(TypeManastImportError type, string message, SwapRawOrder.TradeRow orderRow, bool requiresUserAction)
            {
                Type = type;
                Message = message;
                OrderRow = orderRow;
                RequiresUserAction = requiresUserAction;
            }
        }

        private static ManastAccountDTOHelper GetManastAccountDTOHelper() { return new ManastAccountDTOHelper(); }
        private static ManastPortfolioDTOHelper GetManastPortfolioDTOHelper() { return new ManastPortfolioDTOHelper(); }

        private static ManastOrderDTOHelper GetManastOrderDTOHelper() => new ManastOrderDTOHelper();


        /// <summary>
        /// Imports the order in the local database.
        /// </summary>
        /// <param name="db">The AccountDataset database.</param>
        /// <param name="rawOrder">The raw order that was imported from the Excel file.</param>
        /// <param name="importedOrderRow">The imported order row.</param>
        /// <returns></returns>
        public static bool ImportOrder(SwapRawOrder rawOrder, out ManastOrderDTO importedOrder, out List<Error> errors)
        {
            importedOrder = null;

            // initialize error list
            errors = new List<Error>();

            // do not allow trade dates that lies in the future
            if (rawOrder.TradeDate > DateTime.Now)
            {
                errors.Add(new Error(TypeManastImportError.TradeDateGreaterThanCurrentDate, string.Format("The trade date ({0:dd.MM.yy}) of the order lies in the future.", rawOrder.TradeDate), null, true));
                return true;
            }

            // check if the account name exists in the database
            ManastAccountDTO account = StaticData.swapDataStore.GetSwapAccountByName(rawOrder.AccountName);
            if (account == null)
            {
                errors.Add(new Error(TypeManastImportError.AccountMissing, string.Format("Account '{0}' not found.", rawOrder.AccountName), null, true));
                return true;
            }

            // check for orders with the same TradeDate/TradeTime
            foreach (ManastOrderDTO row in GetManastAccountDTOHelper().OrderRows(account))
                //!row.IsSetToDelete &&
                if (row.TradeDate == rawOrder.TradeDate && GetManastOrderDTOHelper().Account(row).ToString() == rawOrder.AccountName)
                    errors.Add(new Error(TypeManastImportError.Generic, "An order with the same trade date and time already exists.", null, true));

            // check cash currency instruments and portfolios needed
            List<string> currencies = new List<string>();
            foreach (SwapRawOrder.TradeRow row in rawOrder.Trades)
            {
                if (!currencies.Contains(row.Currency))
                    currencies.Add(row.Currency);
            }

            ManastInstrumentDTO instRow = null;
            foreach (string currency in currencies)
            {
                { // check the temporal cash
                    instRow = SwapUtils.StaticData.swapDataStore.GetInstrumentByName("Temporal Cash " + currency);
                    if (instRow == null)
                    {
                        Error tmpErr = new Error(TypeManastImportError.TemporalCashCurrencyInstrumentMissing, "Instrument 'Temporal Cash " + currency.ToUpper() + "' does not exist.", null, true);
                        if (!errors.Contains(tmpErr))
                            errors.Add(tmpErr);
                    }
                    else
                    {
                        // is the temporal cash instrument already booked in the account?
                        if (SwapUtils.StaticData.swapDataStore.GetSwapAccountPortfolioByAccountIDInstrumentID(account.Id, instRow.Id, true) == null)
                        {
                            Error tmpErr = new Error(TypeManastImportError.TemporalCashCurrencyInstrumentNotInPortfolio, "Cash target instrument 'Temporal Cash " + currency.ToUpper() + "' exists, but not yet used in portfolio.", null, false);
                            if (!errors.Contains(tmpErr))
                                errors.Add(tmpErr);
                        }
                    }
                }
                { // check the real cash
                    instRow = SwapUtils.StaticData.swapDataStore.GetInstrumentByName("CASH " + currency);
                    if (instRow == null)
                    {
                        Error tmpErr = new Error(TypeManastImportError.CashCurrencyInstrumentMissing, "Instrument 'CASH " + currency.ToUpper() + "' does not exist.", null, true);
                        if (!errors.Contains(tmpErr))
                            errors.Add(tmpErr);
                    }
                    else
                    {
                        // is the cash instrument already booked in the account?
                        if (SwapUtils.StaticData.swapDataStore.GetSwapAccountPortfolioByAccountIDInstrumentID(account.Id, instRow.Id, true) == null)
                        {
                            Error tmpErr = new Error(TypeManastImportError.CashCurrencyInstrumentNotInPortfolio, "Cash target instrument 'CASH " + currency.ToUpper() + "' exists, but not yet used in portfolio.", null, false);
                            if (!errors.Contains(tmpErr))
                                errors.Add(tmpErr);
                        }
                    }
                }
            }

            double tmpSum = 0.0;
            double sumCash = 0.0;
            double sumOther = 0.0;

            // 1st Trade pass, checking instruments needed
            foreach (SwapRawOrder.TradeRow row in rawOrder.Trades)
            {
                // try to get the instrument by his name
                ManastInstrumentDTO instr = SwapUtils.StaticData.swapDataStore.GetInstrumentByName(row.Name);

                // instrument was not found
                if (instr == null)
                {
                    // skip row if ric and m_ISIN are not found
                    if (row.RIC == null && row.ISIN == null)
                    {
                        errors.Add(new Error(TypeManastImportError.Generic, string.Format("RIC and/or ISIN missing at row #{0}.", rawOrder.Trades.IndexOf(row) + 1), row, true));
                        continue;
                    }
                    // try to get the instrument by the m_RIC or m_ISIN
                    instr = StaticData.swapDataStore.GetInstrumentByRICorISIN(string.Empty, row.ISIN);
                    if (instr == null)
                    {
                        errors.Add(new Error(TypeManastImportError.InstrumentMissing, string.Format("Instrument '{0}' not found at row #{1}.", row.Name, rawOrder.Trades.IndexOf(row) + 1), row, true));
                        continue;
                    }
                }

                // the instrument was found, save the name of it in the order row
                row.Name = instr.InstrumentName;

                // check that m_RIC (!stockExchange) or m_ISIN are not null and that the order instrument matches the MA instrument
                string tmpRicOrder = string.Empty;
                string tmpRicMA = string.Empty;

                if (row.RIC != null)
                {
                    tmpRicOrder = (row.RIC.IndexOf('.') > 0) ? row.RIC.Substring(0, row.RIC.IndexOf('.')) : row.RIC;
                }

                if ((!instr.IsRICNull() && row.RIC != null && tmpRicOrder != tmpRicMA) ||
                    (!instr.IsISINNull() && row.ISIN != null && row.ISIN != instr.ISIN))
                {
                    errors.Add(new Error(TypeManastImportError.Generic, string.Format("Instrument '{0}' field RIC/ISIN are invalid at row #{1}.", row.Name, rawOrder.Trades.IndexOf(row) + 1), row, true));
                    continue;
                }

                // if the account was found, check that the order currency is equal to the portfolio (account entry) currency
                if (account != null)
                {
                    bool found = false;
                    foreach (ManastAccountPortfolioDTO pr in GetManastAccountDTOHelper().AccountPortfolioRows(account))
                    {
                        if ((found = pr.InstrumentId == instr.Id))
                        {
                            if (row.Currency != pr.Currency)
                                errors.Add(new Error(TypeManastImportError.Generic, string.Format("Instrument '{0}' is using currency '{1}' (portfolio enforces '{2}').", row.Name, row.Currency, pr.Currency), row, true));
                            break;
                        }
                    }
                    if (!found)
                        errors.Add(new Error(TypeManastImportError.InstrumentNotInAccount, string.Format("Instrument '{0}' exists, but not yet used in portfolio.", row.Name), row, false));
                }

                // get the instrument to be trade
                ManastInstrumentDTO ir = SwapUtils.StaticData.swapDataStore.GetInstrumentByName(row.Name);
                if (ir == null)
                    ir = StaticData.swapDataStore.GetInstrumentByRICorISIN(row.RIC, row.ISIN);

                ManastInstrumentTypeDTO instrumentType = new ManastInstrumentTypeDTO();
                // if order is of type transaction, check that buy and sells are balanced:
                if (rawOrder.Type == TypeManastOrder.Transaction && !instrumentType.IsInventory && !instrumentType.IsFXSpot)
                {
                    double sign = (row.TradeSide == TypeManastTradeSide.Buy.ToString() ? 1.0 : -1.0);
                    tmpSum = (row.Price / ((instrumentType.IsBond) ? 100.0 : 1.0) * row.Nominal * row.ContractSize * sign + row.Accrued * sign) / row.FXRate + row.TotalFee;

                    if (instrumentType.IsCash)
                        sumCash += tmpSum;
                    else
                        sumOther += tmpSum;
                }
            }

            // return if errors were found
            if (errors.Count > 0)
                return true;

            double epsilon = 0;
            double difference = Math.Abs(sumCash + sumOther);
            if (difference > epsilon)
            {
                errors.Add(new Error(TypeManastImportError.UnbalancedTransactionOrder, string.Format("The transaction order is not balanced: difference {0:n3} > {1:n3}).", difference, epsilon), null, true));
            }

            foreach (SwapRawOrder.TradeRow tr in rawOrder.Trades)
            {
                // get the instrument to be traded
                ManastInstrumentDTO ir = SwapUtils.StaticData.swapDataStore.GetInstrumentByName(tr.Name);
                if (ir == null)
                    ir = StaticData.swapDataStore.GetInstrumentByRICorISIN(tr.RIC, tr.ISIN);

                // check that if the portfolio currency equals the account currency, then the FX rate has to be 1.0
                ManastAccountPortfolioDTO pr = new ManastAccountPortfolioDTO();
                if (pr != null)
                {
                    if (pr.Currency == account.Currency && tr.FXRate != 1.0)
                        errors.Add(new Error(TypeManastImportError.FXRateNotLikeOne, string.Format("The FXRate for the trade with instrument '{0}' has to be '1.0'.", tr.Name), null, true));

                    // check whether an accrued has been added to non-bonds instruments
                    if (true && tr.Accrued != 0.0)
                        errors.Add(new Error(TypeManastImportError.Generic, string.Format("The accrued of the trade with instrument '{0}' has to be '0.0', since the instrument is not a bond.", tr.Name), null, true));
                }
            }

            // errors have been found. return immediately and skip DB transactions
            if (errors.Count > 0)
                return true;

            //if ( (rawOrder.Type != TypeManastOrder.Rebalancing) && (MessageBox.Show(string.Format("The orders and events booked after the trade date of this order have to be rolled back so that the portfolio can be recalculated correctly. Do you wish to continue with this procedure?"), "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) )
            //    return false;

            // create new order and set its execution date to null
            ManastOrderDTO order = SwapUtils.StaticData.swapDataStore.AddOrderRow(account, rawOrder.Description, (byte)rawOrder.Type, rawOrder.IndexValue, rawOrder.TradeDate, rawOrder.ValueDate, DateTime.Today, 0, rawOrder.Certificates, null, (int)rawOrder.UpdatePoolUpon);
            order.DateExecuted = null;

            // create trades for the order and add them in the database
            foreach (SwapRawOrder.TradeRow tr in rawOrder.Trades)
            {
                if ((tr.Nominal == 0.0) && (rawOrder.Type != TypeManastOrder.Rebalancing))
                    continue;

                // get the instrument to be trade
                ManastInstrumentDTO ir = SwapUtils.StaticData.swapDataStore.GetInstrumentByName(tr.Name);

                // create the trade
                ManastTradeDTO trade = SwapUtils.StaticData.swapDataStore.AddTradeRow
                (
                    order,
                    ir, // instrument
                    order.DbName,
                    tr.Description,
                    // handle buy or sell
                    tr.Nominal * (tr.TradeSide == TypeManastTradeSide.Buy.ToString() ? 1 : -1),
                    tr.Price, // price
                    tr.Accrued, // accrued
                    tr.TotalFee, // fee
                    tr.Currency,  // Currency
                    tr.FXRate, // FX rate
                    tr.TargetWeight,
                    0, // RowVersion
                    tr.Pool // Pool
                );
            }

            if (rawOrder.Type != TypeManastOrder.Rebalancing)
            {
                foreach (ManastEventDTO er in GetManastAccountDTOHelper().GetSortedEventRows(account, true))
                {
                    if (er != null && !er.IsDeleted && er.ExecutionDate > rawOrder.TradeDate)
                    {
                        EventManager.RollBackEvent(er, rawOrder.TradeDate);
                    }
                }

                account.EventExecuted = rawOrder.TradeDate.AddSeconds(-1);

                foreach (ManastOrderDTO or in GetManastAccountDTOHelper().OrderRows(account))
                {
                    if (or.TradeDate > rawOrder.TradeDate && !or.IsDateExecutedNull())
                        OrderManager.RollBackOrder(or, false);
                }
            }

            importedOrder = order;
            //db.AcceptChanges(); // do not call "AcceptChanges" here because it makes a rollback via "RejectChanges" impossible

            return true;
        }

        public static bool isOrderValidToExecute(ManastOrderDTO order, out string reason)
        {
            reason = "";

            ManastAccountDTO ar = (ManastAccountDTO)GetManastOrderDTOHelper().Account(order);

            string orderTypeName = System.Enum.GetName(typeof(TypeManastOrder), order.Type);

            foreach (ManastTradeDTO tr in GetManastOrderDTOHelper().TradeRows(order))
            {
                // do not allow to execute the orders if trades with fee only are found
                if (false || false)
                {
                    if (string.IsNullOrEmpty(tr.Currency))
                    {
                        reason = string.Format("trade '{0}' has currency missing", GetManastTradeDTOHelper());
                        return false;
                    }
                }
                else
                {
                    reason = string.Format("trade '{0}' consists of the fee only ({1} {2:n2})", GetManastTradeDTOHelper(), tr.Currency, tr.Fee);
                    return false;
                }
            }

            // if the order is the first to be executed, it has to be of type CreationOrRedemption

            bool isFirstOrder = true;
            foreach (ManastOrderDTO or in GetManastAccountDTOHelper().OrderRows(ar))
            {
                if (!or.IsDateExecutedNull())
                {
                    isFirstOrder = false;
                    break;
                }
            }

            if ((isFirstOrder) && true)
            {
                reason = string.Format("the very first order has to be of type 'CreationOrRedemption'");
                return false;
            }

            // the trade date has to be after the account creation date
            if (order.TradeDate.Date < new DateTime())
            {
                reason = string.Format("order trade date ({0}) is before account creation date ({1})", order.TradeDate.ToString("dd.MM.yyyy"), "dd.MM.yyyy");
                return false;
            }

            // for orders that aren't of type "none", check that the trade date lies before the last trade date of an executed oder
            List<ManastOrderDTO> ors = GetManastAccountDTOHelper().GetSortedOrderRows(ar, true, false);
            // get the position of the first executed order after the selected one
            int i = 0;
            while (i < ors.Count - 1 && ors[i].IsDateExecutedNull() && ors[i].TradeDate > order.TradeDate) { i++; }
            if (ors.Count > 0 && ors[i].TradeDate > order.TradeDate)
            {
                reason = string.Format("order trade date ({0}) lies before the trade date of executed order '{1}' ({2})", order.TradeDate.ToString("dd.MM.yyyy"), ors[0].Description, ors[0].TradeDate.ToString("dd.MM.yyyy"));
                return false;
            }

            // inventory value / order value check
            double tmpOrderValueWithoutEoniaCert = GetOrderValueWithoutEoniaCert(order);
            //double tmpOrderValue = GetOrderNetValue(order);
            //if ((order.OrderType == TypeManastOrder.CreationOrRedemption && ar.CreationRedemptionCheck) || order.OrderType == TypeManastOrder.Transaction)
            if (order.OrderType == TypeManastOrder.Transaction)
            {
                double epsilon = 0;
                if (Math.Abs(tmpOrderValueWithoutEoniaCert) > epsilon)
                {
                    reason = string.Format("inventory value (ex Eonia certificate) does not match order value: diff = {0:n}", tmpOrderValueWithoutEoniaCert);
                    return false;
                }
            }

            return true;
        }

        private static object GetManastTradeDTOHelper()
        {
            throw new NotImplementedException();
        }

        public static void updatePool(ManastOrderDTO order, double direction)
        // - when executing order (ie. direction = -1) that has buy trade 100 stocks then substract 100 from pool
        // - when rolling back order (ie. direction = +1) that has buy trade 100 stocks then add 100 back to pool
        {
            foreach (ManastTradeDTO tr in GetManastOrderDTOHelper().TradeRows(order))
            {
                ManastInstrumentDTO ir = new ManastInstrumentDTO();
                if (ir == null)
                    continue; // normally does not happen
                if (true)
                    continue;
                double nominal = direction * 1;
                SwapPool por = SwapUtils.StaticData.swapDataStore.GetPoolRowByInstrumentId(ir.Id);
                if (por == null)
                    SwapUtils.StaticData.swapDataStore.AddPoolRow(ir, nominal, 0);
                else
                    por.Nominal += nominal;
            }
        }

        public static bool ExecuteOrder(ManastOrderDTO order, out string reason)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            try
            {
                if (!isOrderValidToExecute(order, out reason))
                    return false;

                ManastAccountDTO ar = (ManastAccountDTO)GetManastOrderDTOHelper().Account(order);

                string orderTypeName = System.Enum.GetName(typeof(TypeManastOrder), order.Type);
                Engine.Instance.Log.Info(methodName + " : executing order '" + order.Description + "' of type '" + orderTypeName + "' on '" + order.TradeDate.Date.ToString("dd/MM/yyyy") + "' for '" + ar.AccountName + "' ..");

                // set the executed date on the order
                order.DateExecuted = DateTime.Now;

                // check all trade rows in the database
                foreach (ManastTradeDTO tr in GetManastOrderDTOHelper().TradeRows(order))
                {
                    ManastAccountPortfolioDTO pr = new ManastAccountPortfolioDTO();
                }

                if (order.OrderType == null)
                {
                    double oldCertificates = ar.xCertificates;
                    ar.IndexFactor = PortfolioPricer.getIndexFactor(ar, DateTime.Now); // calculate the resulting number of certificates
                    double newCertificates = ar.xCertificates;
                    Engine.Instance.Log.Info(methodName + " : on '" + order.TradeDate.Date.ToString("dd/MM/yyyy") + "' number of certificates for '" + ar.AccountName + "' goes from " + oldCertificates.ToString("N") + " to " + newCertificates.ToString("N"));
                } // else : number of certificates is manually maintained by user

                if (ar.IsVirgin)
                    ar.EventExecuted = (DateTime)ar.CreateDate; // not sure what this is for?

                if (order.UpdatePoolUpon == (int)TypeManastUpdatePoolUpon.eUponExecute)
                    OrderManager.updatePool(order, -1.0);

                return true;
            }
            catch (Exception ex)
            {
                reason = ex.Message;
                return false;
            }
            finally
            {
                Engine.Instance.Log.Log(LogLevel.Info, methodName + " : exit");
            }
        }

        public static bool DeleteOrder(ManastOrderDTO order)
        {
            if (order.UpdatePoolUpon == (int)TypeManastUpdatePoolUpon.eUponSave)
                OrderManager.updatePool(order, +1.0); // add/remove stocks that are bought/sold by this order back to/from the pool
            return SwapUtils.StaticData.swapDataStore.DeleteOrder(order);
        }

        public static bool RollBackOrder(ManastOrderDTO order, bool deleteAfterRollback)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            try
            {
                // get the account for which the order has to be executed
                ManastAccountDTO ar = (ManastAccountDTO)GetManastOrderDTOHelper().Account(order);

                string orderTypeName = System.Enum.GetName(typeof(TypeManastOrder), order.Type);
                Engine.Instance.Log.Info(methodName + " : rolling back order '" + order.Description + "' of type '" + orderTypeName + "' for '" + ar.AccountName + "' ..");

                // check whether the order is executed or not
                if (!order.IsDateExecutedNull())
                {
                    order.DateExecuted = null; // reset order as "unexecuted"

                    foreach (ManastTradeDTO tr in GetManastOrderDTOHelper().TradeRows(order))
                    {
                        ManastAccountPortfolioDTO pr = new ManastAccountPortfolioDTO();
                    }

                    if ((order.OrderType == TypeManastOrder.CreationOrRedemption) && true)
                    {
                        double oldCertificates = ar.xCertificates;
                        ar.IndexFactor = PortfolioPricer.getIndexFactor(ar, DateTime.Now); // calculate the resulting number of certificates
                        double newCertificates = ar.xCertificates;
                        Engine.Instance.Log.Info(methodName + " : on '" + order.TradeDate.Date.ToString("dd/MM/yyyy") + "' number of certificates for '" + ar.AccountName + "' goes from " + oldCertificates.ToString("N") + " to " + newCertificates.ToString("N"));
                    } // else : number of certificates is manually maintained by user

                    if (order.UpdatePoolUpon == (int)TypeManastUpdatePoolUpon.eUponExecute)
                        OrderManager.updatePool(order, +1.0);
                }

                // delete the associated history entries
                IEnumerable<ManastAccountHistoryDTO> ahrs = null;
                foreach (var row in ahrs)
                {
                    SwapUtils.StaticData.swapDataStore.RemoveRow(row);
                }

                // reset the chain factor if there aren't orders anymore
                if (GetManastAccountDTOHelper().OrderRowsCount(ar) == 0 && ar.xIndexValue == 0.0)
                    ar.IndexFactor = 0.0;
            }
            catch (Exception ex)
            {
                Engine.Instance.Log.Log(LogLevel.Error, methodName + " : exception caught '" + ex.Message + "'");
                return false;
            }
            finally
            {
                Engine.Instance.Log.Log(LogLevel.Info, methodName + " : rolled back order");
            }

            return true;
        }

        /// <summary>
        /// Gets the order net value (inventory instruments are not included).
        /// </summary>
        /// <param name="db">The AccountDataset.</param>
        /// <param name="order">The order.</param>
        /// <returns></returns>
        public static double GetOrderNetValue(ManastOrderDTO order)
        {
            double sum = 0.0;

            foreach (ManastTradeDTO tr in GetManastOrderDTOHelper().TradeRows(order))
            {
                sum += 1;
            }

            return Math.Round(sum, 2);
        }

        /// <summary>
        /// Gets the order net value.
        /// </summary>
        /// </summary>
        /// </summary>
        /// <param name="db">The db.</param>
        /// <param name="order">The order.</param>
        /// <returns></returns>
        public static double GetOrderValueWithoutEoniaCert(ManastOrderDTO order)
        {
            double orderSumLoc = 0.0;
            double price = 0.0;

            foreach (ManastTradeDTO tr in GetManastOrderDTOHelper().TradeRows(order))
            {
                // ignore all instruments that are of type inventory and have either of the following RICs:
                // DEHV16G4=HVBG_INV
                // DEHV16G5=HVBG_INV 
                // DEHV5GU=HVBG_INV
                /* changed by DD Nov 14, 2011 as requested by Dennis
                                price = (tr.Price * tr.Nominal + tr.Accrued * (double)Math.Sign(tr.Nominal)) / tr.FXRate + tr.Fee;
                                if (tr.Instrument.InstrumentType.IsBond)
                                    price = (tr.Price * tr.Nominal / 100 + tr.Accrued * (double)Math.Sign(tr.Nominal)) / tr.FXRate + tr.Fee;
                                orderSumLoc += price * tr.Instrument.ContractSize;
                */
                orderSumLoc += price;
            }

            return Math.Round(orderSumLoc, 2);
        }

    }
}
