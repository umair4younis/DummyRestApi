using NLog;
using Puma.MDE.Data;
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

        /// <summary>
        /// Imports the order in the local database.
        /// </summary>
        /// <param name="db">The AccountDataset database.</param>
        /// <param name="rawOrder">The raw order that was imported from the Excel file.</param>
        /// <param name="importedOrderRow">The imported order row.</param>
        /// <returns></returns>
        public static bool ImportOrder(SwapRawOrder rawOrder, out SwapOrder importedOrder, out List<Error> errors)
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
            SwapAccount account = GetSwapAccountByName(rawOrder.AccountName);
            if (account == null)
            {
                errors.Add(new Error(TypeManastImportError.AccountMissing, string.Format("Account '{0}' not found.", rawOrder.AccountName), null, true));
                return true;
            }

            // check for orders with the same TradeDate/TradeTime
            //foreach (SwapOrder row in DataAccess.SwapAccounts.GetAccountOrderRows(account.Id))
            foreach (SwapOrder row in account.OrderRows)
                //!row.IsSetToDelete &&
                if ( row.TradeDate == rawOrder.TradeDate && row.Account.AccountName == rawOrder.AccountName)
                    errors.Add(new Error(TypeManastImportError.Generic, "An order with the same trade date and time already exists.", null, true));

            // check cash currency instruments and portfolios needed
            List<string> currencies = new List<string>();
            foreach (SwapRawOrder.TradeRow row in rawOrder.Trades)
            {
                if (!currencies.Contains(row.Currency))
                    currencies.Add(row.Currency);
            }

            SwapAccountInstrument instRow = null;
            foreach (string currency in currencies)
            {
                { // check the temporal cash
                    instRow = GetInstrumentByName("Temporal Cash " + currency);
                    if (instRow == null)
                    {
                        Error tmpErr = new Error(TypeManastImportError.TemporalCashCurrencyInstrumentMissing, "Instrument 'Temporal Cash " + currency.ToUpper() + "' does not exist.", null, true);
                        if (!errors.Contains(tmpErr))
                            errors.Add(tmpErr);
                    }
                    else
                    {
                        // is the temporal cash instrument already booked in the account?
                        if(GetSwapAccountPortfolioByAccountIDInstrumentID(account.Id, instRow.Id, true) == null)
                        {
                            Error tmpErr = new Error(TypeManastImportError.TemporalCashCurrencyInstrumentNotInPortfolio, "Cash target instrument 'Temporal Cash " + currency.ToUpper() + "' exists, but not yet used in portfolio.", null, false);
                            if (!errors.Contains(tmpErr))
                                errors.Add(tmpErr);
                        }
                    }
                }
                { // check the real cash
                    instRow = GetInstrumentByName("CASH " + currency);
                    if (instRow == null)
                    {
                        Error tmpErr = new Error(TypeManastImportError.CashCurrencyInstrumentMissing, "Instrument 'CASH " + currency.ToUpper() + "' does not exist.", null, true);
                        if (!errors.Contains(tmpErr))
                            errors.Add(tmpErr);
                    }
                    else
                    {
                        // is the cash instrument already booked in the account?
                        if(GetSwapAccountPortfolioByAccountIDInstrumentID(account.Id, instRow.Id, true) == null)
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
                SwapAccountInstrument instr = GetInstrumentByName(row.Name);

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
                    instr = GetInstrumentByRICorISIN(string.Empty, row.ISIN);
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
                    tmpRicMA = (instr.RIC.IndexOf('.') > 0) ? instr.RIC.Substring(0, instr.RIC.IndexOf('.')) : instr.RIC;
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
                    foreach (SwapAccountPortfolio pr in account.AccountPortfolioRows)
                    {
                        if ((found = pr.Instrument.Id == instr.Id))
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
                SwapAccountInstrument ir = GetInstrumentByName(row.Name);
                if (ir == null)
                    ir = GetInstrumentByRICorISIN(row.RIC, row.ISIN);

                // if order is of type transaction, check that buy and sells are balanced:
                if (rawOrder.Type == TypeManastOrder.Transaction && !ir.InstrumentType.IsInventory && !ir.InstrumentType.IsFXSpot)
                {
                    double sign = (row.TradeSide == TypeManastTradeSide.Buy.ToString() ? 1.0 : -1.0);
                    tmpSum = (row.Price / ((ir.InstrumentType.IsBond) ? 100.0 : 1.0) * row.Nominal * row.ContractSize * sign + row.Accrued * sign) / row.FXRate + row.TotalFee;

                    if (ir.InstrumentType.IsCash)
                        sumCash += tmpSum;
                    else
                        sumOther += tmpSum;
                }
            }

            // return if errors were found
            if (errors.Count > 0)
                return true;

            double epsilon = getToleratedOrderUnbalance();
            double difference = Math.Abs(sumCash + sumOther);
            if (difference > epsilon)
            {
                errors.Add(new Error(TypeManastImportError.UnbalancedTransactionOrder, string.Format("The transaction order is not balanced: difference {0:n3} > {1:n3}).", difference, epsilon), null, true));
            }

            foreach (SwapRawOrder.TradeRow tr in rawOrder.Trades)
            {
                // get the instrument to be traded
                SwapAccountInstrument ir = GetInstrumentByName(tr.Name);
                if (ir == null)
                    ir = GetInstrumentByRICorISIN(tr.RIC, tr.ISIN);

                // check that if the portfolio currency equals the account currency, then the FX rate has to be 1.0
                SwapAccountPortfolio pr = account.GetSwapAccountPortfolioByInstrumentId(ir.Id);
                if (pr != null)
                {
                    if (pr.Currency == account.Currency && tr.FXRate != 1.0)
                        errors.Add(new Error(TypeManastImportError.FXRateNotLikeOne, string.Format("The FXRate for the trade with instrument '{0}' has to be '1.0'.", tr.Name), null, true));

                    // check whether an accrued has been added to non-bonds instruments
                    if (!pr.Instrument.InstrumentType.IsBond && tr.Accrued != 0.0)
                        errors.Add(new Error(TypeManastImportError.Generic, string.Format("The accrued of the trade with instrument '{0}' has to be '0.0', since the instrument is not a bond.", tr.Name), null, true));
                }
            }

            // errors have been found. return immediately and skip DB transactions
            if (errors.Count > 0)
                return true;

            //if ( (rawOrder.Type != TypeManastOrder.Rebalancing) && (MessageBox.Show(string.Format("The orders and events booked after the trade date of this order have to be rolled back so that the portfolio can be recalculated correctly. Do you wish to continue with this procedure?"), "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) )
            //    return false;

            // create new order and set its execution date to null
            SwapOrder order = AddOrderRow(account, rawOrder.Description, (byte)rawOrder.Type, rawOrder.IndexValue, rawOrder.TradeDate, rawOrder.ValueDate, DateTime.Today, 0, rawOrder.Certificates, FindByID(1), (int)rawOrder.UpdatePoolUpon);
            order.SetDateExecutedNull();

            // create trades for the order and add them in the database
            foreach (SwapRawOrder.TradeRow tr in rawOrder.Trades)
            {
                if ((tr.Nominal == 0.0) && (rawOrder.Type != TypeManastOrder.Rebalancing))
                    continue;

                // get the instrument to be trade
                SwapAccountInstrument ir = GetInstrumentByName(tr.Name);

                // create the trade
                SwapTrade trade = AddTradeRow
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
                foreach (SwapEvent er in account.GetSortedEventRows(true))
                {
                    if (er != null && !er.IsSetToDelete && er.ExecutionDate > rawOrder.TradeDate)
                    {
                        RollBackEvent(er, rawOrder.TradeDate);
                    }
                }

                account.EventExecuted = rawOrder.TradeDate.AddSeconds(-1);

                foreach (SwapOrder or in account.OrderRows)
                {
                    if (or.TradeDate > rawOrder.TradeDate && !or.IsDateExecutedNull())
                        OrderManager.RollBackOrder(or, false);
                }
            }

            importedOrder = order;
            account.NotifyPropertyChanged(nameof(account.OrderRows));
            account.NotifyPropertyChanged(nameof(account.OrderRowsCount));
            account.NotifyPropertyChanged(nameof(account.OrderRowsObservable));
            //db.AcceptChanges(); // do not call "AcceptChanges" here because it makes a rollback via "RejectChanges" impossible

            return true;
        }

        private static void RollBackEvent(SwapEvent er, DateTime tradeDate)
        {
            throw new NotImplementedException();
        }

        private static object FindByID(object mUserID)
        {
            throw new NotImplementedException();
        }

        private static SwapTrade AddTradeRow(SwapOrder order, SwapAccountInstrument ir, string dbName, string description, double v1, double price, double accrued, double totalFee, string currency, double fXRate, double targetWeight, int v2, bool pool)
        {
            throw new NotImplementedException();
        }

        private static SwapOrder AddOrderRow(SwapAccount account, string description, byte type, double indexValue, DateTime tradeDate, DateTime valueDate, DateTime today, int v, double certificates, object value, int updatePoolUpon)
        {
            throw new NotImplementedException();
        }

        private static double getToleratedOrderUnbalance()
        {
            throw new NotImplementedException();
        }

        private static SwapAccountInstrument GetInstrumentByRICorISIN(string empty, string iSIN)
        {
            throw new NotImplementedException();
        }

        private static object GetSwapAccountPortfolioByAccountIDInstrumentID(int id1, int id2, bool v)
        {
            throw new NotImplementedException();
        }

        private static SwapAccountInstrument GetInstrumentByName(string v)
        {
            throw new NotImplementedException();
        }

        private static SwapAccount GetSwapAccountByName(string accountName)
        {
            throw new NotImplementedException();
        }

        public static bool isOrderValidToExecute(SwapOrder order, out string reason)
        {
            reason = "";

            SwapAccount ar = order.Account;

            string orderTypeName = System.Enum.GetName(typeof(TypeManastOrder), order.Type);

            foreach (SwapTrade tr in order.TradeRows)
            {
                // do not allow to execute the orders if trades with fee only are found
                if (tr.Net != 0.0 && tr.Net == tr.Fee)
                {
                    reason = string.Format("trade '{0}' consists of the fee only ({1} {2:n2})", tr.Instrument.InstrumentName, tr.Currency, tr.Fee);
                    return false;
                }
                if (string.IsNullOrEmpty(tr.Currency))
                {
                    reason = string.Format("trade '{0}' has currency missing", tr.Instrument.InstrumentName);
                    return false;
                }
            }

            // if the order is the first to be executed, it has to be of type CreationOrRedemption

            bool isFirstOrder = true;
            foreach (SwapOrder or in ar.OrderRows)
            {
                if (!or.IsDateExecutedNull())
                {
                    isFirstOrder = false;
                    break;
                }
            }

            if ((isFirstOrder) && (order.Type != (int)TypeManastOrder.CreationOrRedemption))
            {
                reason = string.Format("the very first order has to be of type 'CreationOrRedemption'");
                return false;
            }

            // the trade date has to be after the account creation date
            if (order.TradeDate.Date < ar.CreateDate.Date)
            {
                reason = string.Format("order trade date ({0}) is before account creation date ({1})", order.TradeDate.ToString("dd.MM.yyyy"), ar.CreateDate.ToString("dd.MM.yyyy"));
                return false;
            }

            // for orders that aren't of type "none", check that the trade date lies before the last trade date of an executed oder
            List<SwapOrder> ors = ar.GetSortedOrderRows(true, false);
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
                double epsilon = getToleratedOrderUnbalance();
                if (Math.Abs(tmpOrderValueWithoutEoniaCert) > epsilon)
                {
                    reason = string.Format("inventory value (ex Eonia certificate) does not match order value: diff = {0:n}", tmpOrderValueWithoutEoniaCert);
                    return false;
                }
            }

            return true;
        }

        public static void updatePool(SwapOrder order, double direction)
        // - when executing order (ie. direction = -1) that has buy trade 100 stocks then substract 100 from pool
        // - when rolling back order (ie. direction = +1) that has buy trade 100 stocks then add 100 back to pool
        {
            foreach (SwapTrade tr in order.TradeRows)
            {
                if (tr.Pool == false)
                    continue;
                if (tr.Nominal == 0.0)
                    continue;
                SwapAccountInstrument ir = tr.Instrument;
                if (ir == null)
                    continue; // normally does not happen
                if (ir.InstrumentType.IsCash)
                    continue;
                double nominal = direction * tr.Nominal;
                SwapPool por = GetPoolRowByInstrumentId(ir.Id);
                if (por == null)
                    AddPoolRow(ir, nominal, 0); 
                else
                    por.Nominal += nominal;
            }
        }

        private static void AddPoolRow(SwapAccountInstrument ir, double nominal, int v)
        {
            throw new NotImplementedException();
        }

        private static SwapPool GetPoolRowByInstrumentId(int id)
        {
            throw new NotImplementedException();
        }

        public static bool ExecuteOrder(SwapOrder order, out string reason)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            try
            {
                if (!isOrderValidToExecute(order, out reason))
                    return false;

                SwapAccount ar = order.Account;

                string orderTypeName = System.Enum.GetName(typeof(TypeManastOrder), order.Type);
                Engine.Instance.Log.Info(methodName + " : executing order '" + order.Description + "' of type '" + orderTypeName + "' on '" + order.TradeDate.Date.ToString("dd/MM/yyyy") + "' for '" + ar.AccountName + "' ..");

                // set the executed date on the order
                order.DateExecuted = DateTime.Now;

                // check all trade rows in the database
                foreach (SwapTrade tr in order.TradeRows)
                {
                    SwapAccountPortfolio pr = order.Account.GetSwapAccountPortfolioByInstrumentId(tr.Instrument.Id);
                    PortfolioPricer.calculateNominal(ar, pr, DateTime.Now); // calculate resulting Nominal and AveragePrice
                }

                if ((order.OrderType == TypeManastOrder.CreationOrRedemption) && (IsCalculateCertificatesFromOrders()))
                {
                    double oldCertificates = ar.xCertificates;
                    ar.IndexFactor = (decimal)PortfolioPricer.getIndexFactor(ar, DateTime.Now); // calculate the resulting number of certificates
                    double newCertificates = ar.xCertificates;
                    Engine.Instance.Log.Info(methodName + " : on '" + order.TradeDate.Date.ToString("dd/MM/yyyy") + "' number of certificates for '" + ar.AccountName + "' goes from " + oldCertificates.ToString("N") + " to " + newCertificates.ToString("N"));
                } // else : number of certificates is manually maintained by user

                if (ar.IsVirgin)
                    ar.EventExecuted = ar.CreateDate; // not sure what this is for?

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

        private static bool IsCalculateCertificatesFromOrders()
        {
            throw new NotImplementedException();
        }

        public static bool DeleteOrder(SwapOrder order)
        {
            if (order.UpdatePoolUpon == (int)TypeManastUpdatePoolUpon.eUponSave)
                OrderManager.updatePool(order, +1.0); // add/remove stocks that are bought/sold by this order back to/from the pool
            return DeleteOrder(order);
        }

        public static bool RollBackOrder(SwapOrder order, bool deleteAfterRollback)
        {
            string methodName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name + "::" + System.Reflection.MethodBase.GetCurrentMethod().Name;

            try
            {
                // get the account for which the order has to be executed
                SwapAccount ar = order.Account;

                string orderTypeName = System.Enum.GetName(typeof(TypeManastOrder), order.Type);
                Engine.Instance.Log.Info(methodName + " : rolling back order '" + order.Description + "' of type '" + orderTypeName + "' for '" + ar.AccountName + "' ..");

                // check whether the order is executed or not
                if (!order.IsDateExecutedNull())
                {
                    order.SetDateExecutedNull(); // reset order as "unexecuted"

                    foreach (SwapTrade tr in order.TradeRows)
                    {
                        SwapAccountPortfolio pr = order.Account.GetSwapAccountPortfolioByInstrumentId(tr.Instrument.Id);
                        PortfolioPricer.calculateNominal(ar, pr, DateTime.Now);
                    }

                    if ((order.OrderType == TypeManastOrder.CreationOrRedemption) && (IsCalculateCertificatesFromOrders()))
                    {
                        double oldCertificates = ar.xCertificates;
                        ar.IndexFactor = (decimal)PortfolioPricer.getIndexFactor(ar, DateTime.Now); // calculate the resulting number of certificates
                        double newCertificates = ar.xCertificates;
                        Engine.Instance.Log.Info(methodName + " : on '" + order.TradeDate.Date.ToString("dd/MM/yyyy") + "' number of certificates for '" + ar.AccountName + "' goes from " + oldCertificates.ToString("N") + " to " + newCertificates.ToString("N"));
                    } // else : number of certificates is manually maintained by user

                    if (order.UpdatePoolUpon == (int)TypeManastUpdatePoolUpon.eUponExecute)
                        OrderManager.updatePool(order, +1.0);
                }

                // delete the associated history entries
                IEnumerable<SwapAccountHistory> ahrs = GetOrderAccountHistoryRows(order).ToList();
                foreach(var row in ahrs)
                {
                    DeleteSwapAccountHistoryRow(row);
                }

                // either delete or unexecute the order
                if ((deleteAfterRollback || order.Description.StartsWith("Automatic")) && order.IsDateExecutedNull())
                {
                    DeleteOrder(order);
                }

                // reset the chain factor if there aren't orders anymore
                if (ar.OrderRowsCount == 0 && ar.xIndexValue == 0.0)
                    ar.IndexFactor = (decimal)0.0;
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

        private static void DeleteSwapAccountHistoryRow(SwapAccountHistory row)
        {
            throw new NotImplementedException();
        }

        private static IEnumerable<SwapAccountHistory> GetOrderAccountHistoryRows(SwapOrder or)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the order net value (inventory instruments are not included).
        /// </summary>
        /// <param name="db">The AccountDataset.</param>
        /// <param name="order">The order.</param>
        /// <returns></returns>
        public static double GetOrderNetValue(SwapOrder order)
        {
            double sum = 0.0;

            foreach (SwapTrade tr in order.TradeRows)
            {
                // ignore inventory instruments
                if ((tr.Instrument.InstrumentType.IsInventory) || (tr.Instrument.InstrumentType.IsFXSpot))
                    continue;
                sum += tr.Net;
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
        public static double GetOrderValueWithoutEoniaCert(SwapOrder order)
        {
            double orderSumLoc = 0.0;
            double price = 0.0;

            foreach (SwapTrade tr in order.TradeRows)
            {
                // ignore all instruments that are of type inventory and have either of the following RICs:
                // DEHV16G4=HVBG_INV
                // DEHV16G5=HVBG_INV 
                // DEHV5GU=HVBG_INV
                if (tr.Instrument.InstrumentType.IsInventory && (tr.Instrument.RIC == "DEHV16G4=HVBG_INV" || tr.Instrument.RIC == "DEHV16G5=HVBG_INV" || tr.Instrument.RIC == "DEHV5GU=HVBG_INV"))
                    continue;
                if (tr.Instrument.InstrumentType.IsFXSpot)
                    continue;
                /* changed by DD Nov 14, 2011 as requested by Dennis
                                price = (tr.Price * tr.Nominal + tr.Accrued * (double)Math.Sign(tr.Nominal)) / tr.FXRate + tr.Fee;
                                if (tr.Instrument.InstrumentType.IsBond)
                                    price = (tr.Price * tr.Nominal / 100 + tr.Accrued * (double)Math.Sign(tr.Nominal)) / tr.FXRate + tr.Fee;
                                orderSumLoc += price * tr.Instrument.ContractSize;
                */
                price = (tr.Price * tr.Nominal + tr.Accrued * (double)Math.Sign(tr.Nominal)) / tr.FXRate;
                if (tr.Instrument.InstrumentType.IsBond)
                    price = (tr.Price * tr.Nominal / 100 + tr.Accrued * (double)Math.Sign(tr.Nominal)) / tr.FXRate;
                price = price * tr.Instrument.ContractSize + tr.Fee;
                orderSumLoc += price;
            }

            return Math.Round(orderSumLoc, 2);
        }

    }
}
