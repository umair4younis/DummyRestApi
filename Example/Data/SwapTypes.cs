
namespace Puma.MDE.Data
{
    /*-------- MANAST TYPES --------*/

    public enum TypeManastCloseAsLast
    {
        TMINUS1 = 0,
        MOSTRECENT = 1,
        DATE = 2,
        CUSTOM = 3
    }

    public enum TypeManastCloseRefDate
    {
        eNoRefDate = 0,
        eTMinus1 = 1,
        eT = 2,
        eSpecificDate = 3
    }

    public enum TypeManastPriceField
    {
        eNoPriceField = -1,
        // real-time
        eRealTimeLast = 0,
        eRealTimeAsk,
        eRealTimeBid,
        // close
        eClose = 20,
        eFixing1,
        eNAV
    }

    public enum TypeManastUpdatePoolUpon
    {
        eDoNotUpdate = 0,
        eUponSave = 1,
        eUponExecute = 2
    }

    public enum TypeManastPriceTip
    {
        // Close Price
        eClosePriceCcyIsManual = 0,
        eClosePriceCcyIsMissing,
        eClosePriceCcyIsOld,
        eCloseFXRateIsMissing,
        eCloseFXRateIsOld,
        // Last Price Ccy
        eLastPriceCcyIsClosePriceCcy,
        eLastPriceCcyIsClose,
        eLastPriceCcyIsManual,
        eLastPriceCcyIsManualFromInstrument,
        eLastPriceCcyIsOld,
        // Last Price
        eLastPriceIsClosePrice,
        eLastFXRateIsManual,
        eLastFXRateIsManualFromInstrument,
        eLastFXRateIsManual2,
        eLastFXRateIsManualFromInstrument2,
        eLastFXRateIsWMFixing,
        eLastFXRateIsMissing,
        eLastFXRateIsOld,
        eLastFXRateIsClose,
        // Volatility
        eVolatilityIsManual,
        eVolatilityIsMissing,
        eVolatilityIsOld,
        // Other
        eMiscellaneous
    }

    public enum TypeManastUSWHT
    {
        eNotApplicable = 0,
        eNotSpecified = 1,
        eYes = 2,
        eNo = 3
    }

    /// <summary>
    /// Defines the manast security code types
    /// </summary>
    public enum TypeManastSecurityCode
    {
        ALL = -1,
        ADO = 0,
        ISIN = 1,
        WKN = 2,
        RIC = 3,
        Bloomberg_ID = 4,
        SEDOL = 5,
        SICOVAM = 6,
        Name = 7,
        SophisReference = 8
    }

    /// <summary>
    /// Defines the manast instrument types
    /// </summary>
    public enum TypeManstInstrument // see table HVB_MANAST_INSTRUMENTTYPE  
    {
        Cash = 0,
        Bond = 1,
        Stock = 2,
        Rate = 3,
        Fund = 4,
        Future = 5,
        FXRate = 6,
        ETF = 7,
        FeedInstrument = 8,
        Certificate = 9,
        Inventory = 10,
        FXFwd = 11,
        FXOpt = 12,
        FXSpread = 13,
        EquityOption = 14,
        FutureCash = 15,
        FXSpot = 16,
        PreciousMetal = 17,
        ADR = 18,
        GDR = 19,
        Index = 20,
        Volatility = 21,
        NAV = 22,
        CashFee = 23,
        Commission = 24,
        Unknown = 99
    }

    /// <summary>
    /// Defines the manast event types
    /// </summary>
    public enum TypeManastEvent
    {
        Interest = 0,   // 0
        Dividend,       // 1
        Coupon,         // 2
        AccruedDEPRECATED, // 3
        ManagementFee1,  // 4
        StockSplit,     // 5
        ISINChange,     // 6
        ProfitOrLoss,   // 7
        PerformanceFee,  // 8
        ManagementFee2,  // 9
        ManagementFee3,  // 10
        ExecutionFee,  // 11
        _871mDividend, // 12
        SwapDividend, // 13
        DividendReinvest, // 14
        _871mDividendReinvest, // 15
    }

    public enum TypeBookDividendMethod
    {
        None = 0,
        Dividend = 1,
        SwapDividend = 2,
        ReinvestPriceIndex = 3,
        ReinvestReturnIndex = 4
    }

    public enum TypeSwapDividendPaymentRule
    {
        None = 0,
        AtReset = 1,
        Immediately = 2
    }

    public enum TypeApplyAlertToColumn
    {
        None = 0,
        MarketWeight = 1,
        MarketWeightCollateral = 2
    }

    public enum TypeComparisonOperator
    {
        GreaterThan = 1,
        SmallerThan = 2
    }

    public enum TypeManagementStyle
    {
        None = 0,
        Active = 1,
        Passive = 2
    }

    public enum TypeTippMultiplierDefinition
    {
        None = 0,
        LinearWithVol = 1,
        Allianz = 2
    }

    public enum TypeTippAllianzFundClass
    {
        ClassI = 0,
        ClassS = 1
    }

    public enum TypeTippDefinition
    {
        None = 0,
        NoAllTimeHighFirstDay = 1,
        AllTimeHigh = 2,
        ResettingAllTimeHigh = 3
    }

    public enum TypeTippActionDefinition
    {
        None = 0,
        BreachRQ = 1,
        BreachRQorIsBeginOfMonth = 2
    }

    public enum TypeTippAssetClassCategory
    {
        Bond = 0,
        Equity = 1
    }

    public enum TypeTippAssetRiskProfile
    {
        High = 0,
        Low = 1
    }

    public enum TypeManagementFeeCalculation
    {
        Accrued = 0,
        Periodic = 1
    }

    public enum TypeManastSophisTradeType
    {
        Reset = 0,
        Increase,
        Decrease,
        Initial,
        Maturity,
        IntraNostro
    }

    /// <summary>
    /// Defines the manast event action types
    /// </summary>
    public enum TypeManastEventAction
    {
        None = 0,   // no action, only event information
        Pay,        // pay a sum to the cash target of the portfolio
        Delete,      // delete a sum (temporal) from the cash target of the portfolio
        OnlyDeltaAdjustment // Only delta adjustment: the amount is added or detract from the cash without causing an index jump
    }

    /// <summary>
    /// Defines the manast order types
    /// </summary>
    public enum TypeManastOrder
    {
        None = 0,
        CreationOrRedemption = 1,
        Transaction = 2,
        Correction = 3,
        CompositionChange = 4,
        //Storno=4, 
        //Pending=5,
        Rebalancing = 6,
        CurrencyTransaction = 7,
        Commission = 8
    }

    /// <summary>
    /// Defines the manast trade side types
    /// </summary>
    public enum TypeManastTradeSide
    {
        Buy = 0,
        Sell
    }

    public enum TypeManastOrderSide
    {
        Unknown = 0,
        Buy,
        Sell,
        Sell_Buy
    }

    public enum TypeManastMop // Calypso market operation : see enum "EMopType" inside "mainline\SophisExtensions\UniBackOffice\Calypso\CalypsoTrade.h"
    {
        None = 0,
        Insert = 1,
        Increase = 2,
        PartialUnwind = 3,
        FullUnwind = 4,
        Maturity = 6,
        Fee = 7,
    };

    /// <summary>
    /// Defines the manast contribute modes
    /// </summary>
    public enum TypeManastContributeMode
    {
        None = 0,
        OnChange,
        Interval,
        FromReport
    };

    public enum TypeManastOrderReportStyle
    {
        None = 0,
        FixedLayout,
        FlexibleLayout
    };

    public enum TypeManastSophisUpload
    {
        UploadSPI,
        UploadNominal
    };

    public enum TypeManastIndexSnapshotMode
    {
        None = 0,
        LastIndex,
        FromReport
    };

    public enum TypeAccruedUntilDate
    {
        ExcludingPricingDate = 0,
        IncludingPricingDate,
        IncludingToday
    };

    public enum TypeAccountPremiumNotionalCalc
    {
        None = 0,
        Standard,
        Certificates,
        SwapNotional
    }

    public enum TypeAccountPremiumDayCalc
    {
        Standard = 0,
        Forward
    }

    /// <summary>
    /// Enumerates the error types that can occur while importing an order
    /// </summary>
    public enum TypeManastImportError
    {
        None = 0,
        AccountMissing,
        InstrumentMissing,
        InstrumentNotInAccount,
        CashCurrencyInstrumentMissing,
        TemporalCashCurrencyInstrumentMissing,
        CashCurrencyInstrumentNotInPortfolio,
        TemporalCashCurrencyInstrumentNotInPortfolio,
        TradeDateGreaterThanCurrentDate,
        UnbalancedTransactionOrder,
        DecimalNominalNotAllowed,
        FXRateNotLikeOne,
        Generic,
        DuplicateInstrument
    };

    /// <summary>
    /// SPI export modes
    /// </summary>
    public enum TypeManastSPIExport
    {
        ALL = 0,
        BOND,
        NO_BOND
    }

    /// <summary>
    /// Enumeration of the corporate actions source types
    /// </summary>
    public enum TypeCorporateActionsSource
    {
        CAT = 0,
        DIVIDENDFILE,
        REUTERS,
        MANUAL
    }

    /// <summary>
    /// Type of field that has to be contributed on Reuters
    /// </summary>
    public enum TypePublishingField
    {
        None,
        Close,
        Last,
        SnapShot,
        Weight_Cash,
        Weight_Bond,
        Weight_Stock,
        Weight_Fund,
        Weight_ETF,
        Weight_Certificate,
        MtM
    };

    /*-------- Sophis TYPES --------*/

    /// <summary>
    /// Enumeration of the movement types according to the Sophis table documentation 5.2.4 (added: Undefined = 0)
    /// </summary>
    public enum TypeSophisMovement
    {
        Undefined = 0,
        PurchaseOrSale,
        Coupon,
        Split,
        BonusIssue,
        TaxCredit,
        CurrencyPurchase,
        Commissions,
        Balance,
        Exercise
    }

}
