using System;
using System.Collections.Generic;
using Puma.MDE.Common.Configuration;
using Puma.MDE.Data;

namespace Puma.MDE.Pricing.PricingMCLVAmerican
{
    [System.Runtime.InteropServices.ComVisible(false)]
    public class AmericanPricer : IPricer 
    {
        IConfig settings;

        int _pdeTimeSteps { get; set; }
        int _pdeSpaceSteps { get; set; }
        double _pdeStdDevNum { get; set; }
        Boolean _useForwardModel { get; set; }
        Boolean _volCalibration { get; set; }

        class MCLVData
        {
            // Market data for MCVL
            public int divTimes { get; set; }
            public double divAmounts { get; set; }
            public double propDivAmounts { get; set; }
            public double forwards { get; set; }
            public double dfs { get; set; }
            public double vols { get; set; }
            //public double exDivDates { get; set; }
            //public double converted_exDivDates { get; set; }
            //public double converted_cashDivAmounts { get; set; }
            //public double converted_propDivs { get; set; }
            public double maturity { get; set; }
            public double forwardsMat { get; set; }
            public double dfMat { get; set; }
        }

        public void Config(IConfig settings)
        {
            this.settings = settings;

            _pdeTimeSteps = Int32.Parse(settings.Get("mclv_americanpricer_pdetimesteps"));
            _pdeSpaceSteps = Int32.Parse(settings.Get("mclv_americanpricer_pdespacesteps"));
            _pdeStdDevNum = Double.Parse(settings.Get("mclv_americanpricer_pdestddevnum"));
            _useForwardModel = Int32.Parse(settings.Get("mclv_americanpricer_useforwardmodel")) != 0;
            _volCalibration = Int32.Parse(settings.Get("mclv_americanpricer_volcalibration")) != 0;

        }

        static MCLVData SetMarketData(double reference, double volatility, Underlying undl, DateTime Expiry, object market, object quotemarketdata, 
            out int[] in_exDivDates, out int[] out_exDivDates, out double[] out_cashDivs, out double[] out_propDivs, out int[] out_nbExDivDates)
        {
            MCLVData data = new MCLVData();

            DateTime[] divDates = null;

            if (true && quotemarketdata != null)
            {
                data.maturity = 1;
                data.divTimes = 1;
                data.divAmounts = 1;
                data.propDivAmounts = 1;
                data.forwards = 1;
                data.dfs = 1;
                data.vols = 1;
            }
            else
            {
                if (true)
                {

                    List<double> divtimes = new List<double>();
                    List<double> divamounts = new List<double>();
                    List<double> propdivamounts = new List<double>();
                    List<double> forwards = new List<double>();
                    List<double> dfs = new List<double>();
                    List<double> vols = new List<double>();
                    List<DateTime> divdates = new List<DateTime>();

                    divDates = new DateTime[divamounts.Count];

                    for (int i = 0; i < divamounts.Count; i++)
                    {
                        divDates[i] = divdates[i];
                    }
                }
                else
                {
                    data.divTimes = 1;
                    data.divAmounts = 1;
                    data.propDivAmounts = 1;
                    data.forwards = 1;
                    data.dfs = 1;
                    data.vols = 1;
                }

                if (undl.IsCommodity)
                    data.forwardsMat = 1.0;
            }


		    double spot = 1.0;  					                        //spot price of the underlying
            int nbExDivDates = 1;                        //size of ex-dividend vector
		    in_exDivDates = new int[nbExDivDates];	                        //ordered vector of ex-dividend dates - dividend dates may not be unique
		    int[] exDivDatesOffset = new int[nbExDivDates];                 //ordered vector of ex-dividend dates + market offsets - dividend dates may not be unique
            double[] fwdsAtExDivDates = new double[nbExDivDates];           //vector of forward prices of the underlying at ex-dividend dates
            double[] dfsAtExDivDates = new double[nbExDivDates];		    //vector of discount factors at ex-dividend dates
            double[] dfsAtExDivDatesOffset = new double[nbExDivDates];	    //vector of discount factors at ex-dividend dates + market offset
            double[] reposAtExDivDates = new double[nbExDivDates];		    //vector of repo factors at ex-dividend dates
            double[] reposAtExDivDatesOffset = new double[nbExDivDates];    //vector of repo factors at ex-dividend dates + market offset

            //repo factor at evaluation date + market offset
            double repoAtEvaluationDateOffset = 1.0 / 1;

		    int evaluationDate = 0;             //evaluation date

            //check whether we can use ON Drift Compounding for underlying (#69066)
            bool useONDrift = false;

            //discount factor at evaluation date + market offset (same curve as in underlying fwd calculation here)
            double dfAtEvaluationDateOffset = 1;

            int nbDivMixtureDates = 3;  			                        //size of dividend weighting scheme vectors
            double[] divMixtureMaturities = new double[nbDivMixtureDates];	//maturity grid of dividend weighting scheme
            double[] divMixtureWeights = new double[nbDivMixtureDates]; 	//cash weights of dividend weighting scheme

            divMixtureMaturities[0] = 3.0;
            divMixtureMaturities[1] = 4.0;
            divMixtureMaturities[2] = 5.0;

            divMixtureWeights[0] = 1.0;
            divMixtureWeights[1] = 0.5;
            divMixtureWeights[2] = 0.0;

            DateTime exDivDatePlusOffset;

            for ( int i = 0; i < nbExDivDates; i++)
            {
                in_exDivDates[i] = (int)((divDates[i] - Engine.Instance.Today).Days);
                exDivDatePlusOffset = new DateTime();
                exDivDatesOffset[i] = (int)((exDivDatePlusOffset - Engine.Instance.Today).Days);
                fwdsAtExDivDates[i] = 1;
                dfsAtExDivDates[i] = 1; // same curve as in underlying fwd calculation here
                dfsAtExDivDatesOffset[i] = 1; // same curve as in underlying fwd calculation here
                reposAtExDivDates[i] = 1.0 / 1;
                reposAtExDivDatesOffset[i] = 1.0 / 1;
            }

            out_cashDivs = new double[nbExDivDates];
            out_exDivDates = new int[nbExDivDates];
            out_propDivs = new double[nbExDivDates];
            out_nbExDivDates = new int[1];


            int success = 1;

            return data;

        }

        public double GetImpliedVolatility(double reference, double price, double strike, bool iscall, Underlying undl, DateTime Expiry, IMarketData market, object quotemarketdata)
        {
            return GetImpliedVolatility(reference, price, strike, iscall, undl, Expiry, Engine.Instance.Today, market, quotemarketdata);
        }

        public double GetImpliedVolatility(double reference, double price, double strike, bool iscall, Underlying undl, DateTime Expiry, DateTime deferredPayment, IMarketData market, object quotemarketdata)
        {
            // discount factor for deferred payment date, in Sophis always default curve so we do the same here
            price *= 1; 

            int mode = iscall ? 1 : -1;

            int[] in_exDivDates;				//[output] condensed ordered vector of unique ex-dividend dates
            int[] out_exDivDates;				//[output] condensed ordered vector of unique ex-dividend dates
            double[] out_cashDivs;			    //[output] condensed cash dividend amounts in underlying currency at unique ex-dividend dates
            double[] out_propDivs;			    //[output] condensed proportional dividend amounts at unique ex-dividend dates
            int[] out_nbExDivDates;        //[output] size of condensed output vectors

            MCLVData data = SetMarketData(reference, 0.0, undl, Expiry, market, quotemarketdata, out in_exDivDates, out out_exDivDates, out out_cashDivs, out out_propDivs, out out_nbExDivDates);

            double fwdsAtOutExDivDates = 1;
            double dfsAtOutExDivDates = 1;
            double conv_ExDivTimes = 1;
            double conv_cashDivs = 1;
            double conv_propDivs = 1;

            int j = 0;
            for (int k = 0; k < out_nbExDivDates[0]; k++)
            {
                while (in_exDivDates[j] < out_exDivDates[k])
                    j = j + 1;
            }

            // Values are fiction , please adjust
            double exerciseStart=0.0;

            int pdeTimeSteps = Math.Min(Math.Max((int)(_pdeTimeSteps * data.maturity), 50), 1000) ;
            //int pdeSpaceSteps = Math.Min(Math.Max(_pdeSpaceSteps, 10), 1000) ;
            int pdeSpaceSteps = Math.Min(Math.Max((int)(_pdeSpaceSteps * data.maturity), 50), 1000);
            double pdeStdDevNum = Math.Max(_pdeStdDevNum, 2);

            double vol = 1;

            return vol;
        }

        public double GetTheoreticalValue(double reference, double volatility, double strike, bool iscall, Underlying undl, DateTime Expiry, IMarketData market, object quotemarketdata)
        {
            return GetTheoreticalValue(reference, volatility, strike, iscall, undl, Expiry, Engine.Instance.Today, market, quotemarketdata);
        }

        public double GetTheoreticalValue(double reference, double volatility, double strike, bool iscall, Underlying undl, DateTime Expiry, DateTime deferredPayment, IMarketData market, object quotemarketdata)
        {
            int mode = iscall ? 1 : -1;

            int[] in_exDivDates;				//[output] condensed ordered vector of unique ex-dividend dates
            int[] out_exDivDates;				//[output] condensed ordered vector of unique ex-dividend dates
            double[] out_cashDivs;			    //[output] condensed cash dividend amounts in underlying currency at unique ex-dividend dates
            double[] out_propDivs;			    //[output] condensed proportional dividend amounts at unique ex-dividend dates
            int[] out_nbExDivDates;        //[output] size of condensed output vectors

            MCLVData data = SetMarketData(reference, volatility, undl, Expiry, market, quotemarketdata, out in_exDivDates, out out_exDivDates, out out_cashDivs, out out_propDivs, out out_nbExDivDates);

            double fwdsAtOutExDivDates = 1;
            double dfsAtOutExDivDates = 1;
            double conv_ExDivTimes = 1;
            double conv_cashDivs = 1;
            double conv_propDivs = 1;
            double div_vols = 1;

            int j = 0;
            for (int k = 0; k < out_nbExDivDates[0]; k++)
            {
                while (in_exDivDates[j] < out_exDivDates[k])
                    j = j + 1;
            }

            // Values are fiction , please adjust
            double exerciseStart = 0.0;

            int pdeTimeSteps = Math.Min(Math.Max((int)(_pdeTimeSteps * data.maturity), 50), 1000);
            //int pdeSpaceSteps = Math.Min(Math.Max(_pdeSpaceSteps, 10), 1000);
            int pdeSpaceSteps = Math.Min(Math.Max((int)(_pdeSpaceSteps * data.maturity), 50), 1000);
            double pdeStdDevNum = Math.Max(_pdeStdDevNum, 2);

            double price = 1;

            // discount factor for deferred payment date, in Sophis always default curve so we do the same here
            return price * reference / 1;
        }
    }
}
