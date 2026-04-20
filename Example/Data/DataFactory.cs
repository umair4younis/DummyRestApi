using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Transform;
using NHibernate.Util;

namespace Puma.MDE.Data
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("76C09675-9A17-43d5-AC4D-71C309B08753")]
    [ComVisible(true)]

    public class DataFactory
    {
        public Object thisLock = new Object();
        
        public enum WorksetStatusEnum
        {
            Disabled = 0,
            ATMOnly  = 1,
            Regular  = 2
        }

        //Ported
        public Underlying GetUnderlying(string reference)
        {
            lock (thisLock )
            {
                Underlying underlying =
                    Engine.Instance.Session.CreateCriteria<Underlying>().Add(
                        Restrictions.Eq("Reference", reference))
                            .UniqueResult<Underlying>();

                return underlying;
            }
        }

        //Ported
        public Underlying GetUnderlyingFromPuma(string reference)
        {
            lock (thisLock )
            {
                Underlying underlying = null;

                var results = Engine.Instance.Session.CreateCriteria<Underlying>().Add(
                        Restrictions.Eq("ORCPumaUnderlying", reference))
                            .List<Underlying>();

                if (results.Count > 0)
                    underlying = results.First();

                return underlying;
            }
        }

        //Ported
        public Underlying GetUnderlyingByISIN(string isin)
        {
            lock (thisLock )
            {
                Underlying underlying = null;

                var results = Engine.Instance.Session.CreateCriteria<Underlying>().Add(
                        Restrictions.Eq("ISIN", isin))
                            .List<Underlying>();

                if (results.Count > 0)
                    underlying = results.First();

                return underlying;
            }
        }

        //Ported
        public Underlying GetUnderlying(int Id)
        {
            lock (thisLock )
            {
                Underlying underlying =
                    Engine.Instance.Session.CreateCriteria<Underlying>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<Underlying>();

                return underlying;
            }
        }

        //Ported
        public Underlyings GetUnderlyings()
        {
            lock (thisLock )
            {
                IList<Underlying> instances =
                    Engine.Instance.Session.CreateCriteria<Underlying>()
                                .List<Underlying>();

                return new Underlyings(instances);
            }
        }

        //Ported
        public Underlyings GetUnderlyingsByType(string Type)
        {
            lock (thisLock )
            {
                IList<Underlying> instances =
                    (IList<Underlying>)Engine.Instance.Session.CreateCriteria<Underlying>()
                        .Add(Restrictions.Eq("Type", Type)).List<Underlying>();

                return new Underlyings(instances);
            }
        }

        public User GetUser(string name)
        {
            lock (thisLock )
            {
                User user =
                    Engine.Instance.Session.CreateCriteria<User>().Add(
                        Restrictions.InsensitiveLike("Name", name))
                            .UniqueResult<User>();

                return user;
            }
        }
        public User GetUser(int Id)
        {
            lock (thisLock )
            {
                User user =
                    Engine.Instance.Session.CreateCriteria<User>().Add(
                        Restrictions.InsensitiveLike("Id", Id))
                            .UniqueResult<User>();

                return user;
            }
        }

        //Ported
        public VolsurfaceModel GetVolsurfaceModel(string name)
        {
            lock (thisLock )
            {
                VolsurfaceModel instance =
                    Engine.Instance.Session.CreateCriteria<VolsurfaceModel>().Add(
                        Restrictions.Eq("Name", name))
                            .UniqueResult<VolsurfaceModel>();

                return instance;
            }
        }

        //Ported
        public VolsurfaceModel GetVolsurfaceModel(int Id)
        {
            lock (thisLock )
            {
                VolsurfaceModel instance =
                    Engine.Instance.Session.CreateCriteria<VolsurfaceModel>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<VolsurfaceModel>();

                return instance;
            }
        }

        //Ported
        [ComVisible(false)]
        public IList<UnderlyingClassification> GetUnderlyingClassifications(Underlying undl)
        {
            lock (thisLock )
            {
                IList<UnderlyingClassification> instance =
                   (IList<UnderlyingClassification>)Engine.Instance.Session.CreateCriteria<UnderlyingClassification>()
                   .Add(Restrictions.Eq("UnderlyingId", 1)).List<UnderlyingClassification>();
                return instance;
            }
        }

        //Ported
        [ComVisible(false)]
        public IList<UnderlyingClassification> GetUnderlyingClassifications(Classification clas)
        {
            lock (thisLock )
            {
                IList<UnderlyingClassification> instance =
                   (IList<UnderlyingClassification>)Engine.Instance.Session.CreateCriteria<UnderlyingClassification>()
                   .Add(Restrictions.Eq("ClassificationId", clas.Id)).List<UnderlyingClassification>();
                return instance;
            }
        }

        //Ported
        public Classification GetClassification(String name)
        {
            lock (thisLock )
            {
                Classification instance =
                    Engine.Instance.Session.CreateCriteria<Classification>().Add(
                        Restrictions.Eq("Name", name))
                            .UniqueResult<Classification>();

                return instance;
            }
        }

        //Ported
        public Classification GetClassification(int Id)
        {
            lock (thisLock )
            {
                Classification instance =
                    Engine.Instance.Session.CreateCriteria<Classification>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<Classification>();

                return instance;
            }
        }

        //Ported
        [ComVisible(false)]
        public IList<ClassificationHierarchie> GetChildClassifications(Classification c)
        {
            lock (thisLock )
            {
                IList<ClassificationHierarchie> instance =
                    (IList<ClassificationHierarchie>)Engine.Instance.Session.CreateCriteria<ClassificationHierarchie>()
                        .Add(Restrictions.Eq("ParentId", c.Id)).List<ClassificationHierarchie>();
                return instance;
            }
        }

        //Ported
        [ComVisible(false)]
        public IList<ClassificationHierarchie> GetParentClassifications(Classification c)
        {
            lock (thisLock )
            {
                IList<ClassificationHierarchie> instance =
                    (IList<ClassificationHierarchie>)Engine.Instance.Session.CreateCriteria<ClassificationHierarchie>()
                        .Add(Restrictions.Eq("ChildId", c.Id)).List<ClassificationHierarchie>();
                return instance;
            }
        }

        //Ported
        public QuoteFilterRule GetFilterRule(Classification c)
        {
            return GetFilterRule(c.Id);
        }

        //Ported
        public QuoteFilterRule GetFilterRule(int classificationId)
        {
            QuoteFilterType type = GetFilterType("Standard");
            return GetFilterRule(classificationId, type.Id);
        }

        //Ported
        public QuoteFilterRule GetFilterRule(Classification c, QuoteFilterType t)
        {
            return GetFilterRule(c.Id, t.Id);
        }

        //Ported
        public QuoteFilterRule GetFilterRule(int classificationId, int typeId)
        {
            lock (thisLock )
            {
                QuoteFilterRule instance =
                    Engine.Instance.Session.CreateCriteria<QuoteFilterRule>()
                    .Add(
                        Restrictions.Eq("ClassificationId", classificationId))
                    .Add(
                        Restrictions.Eq("TypeId", typeId))
                            .UniqueResult<QuoteFilterRule>();

                return instance;
            }
        }

        //Ported
        public QuoteFilterType GetFilterType(int Id)
        {
            lock (thisLock )
            {
                QuoteFilterType instance =
                    Engine.Instance.Session.CreateCriteria<QuoteFilterType>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<QuoteFilterType>();

                return instance;
            }
        }

        //Ported
        public QuoteFilterType GetFilterType(string name)
        {
            lock (thisLock )
            {
                QuoteFilterType instance =
                    Engine.Instance.Session.CreateCriteria<QuoteFilterType>().Add(
                        Restrictions.Eq("Name", name))
                            .UniqueResult<QuoteFilterType>();

                return instance;
            }
        }

        //Ported
        public SettingsSetType GetSettingsSetType(int Id)
        {
            lock (thisLock )
            {
                SettingsSetType instance =
                    Engine.Instance.Session.CreateCriteria<SettingsSetType>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<SettingsSetType>();

                return instance;
            }
        }

        //Ported
        public SettingsSetType GetSettingsSetType(string name)
        {
            lock (thisLock )
            {
                SettingsSetType instance =
                    Engine.Instance.Session.CreateCriteria<SettingsSetType>().Add(
                        Restrictions.Eq("Name", name))
                            .UniqueResult<SettingsSetType>();

                return instance;
            }
        }

        //Ported
        public SettingsSetRule GetSettingsSetRule(Classification c)
        {
            return GetSettingsSetRule(c.Id);
        }

        //Ported
        public SettingsSetRule GetSettingsSetRule(int classificationId)
        {
            QuoteFilterType type = GetFilterType("Standard");
            return GetSettingsSetRule(classificationId, type.Id);
        }

        //Ported
        public SettingsSetRule GetSettingsSetRule(Classification c, SettingsSetType t)
        {
            return GetSettingsSetRule(c.Id, t.Id);
        }

        //Ported
        public SettingsSetRule GetSettingsSetRule(int classificationId, int typeId)
        {
            lock (thisLock )
            {
                SettingsSetRule instance =
                    Engine.Instance.Session.CreateCriteria<SettingsSetRule>()
                    .Add(
                        Restrictions.Eq("ClassificationId", classificationId))
                    .Add(
                        Restrictions.Eq("TypeId", typeId))
                            .UniqueResult<SettingsSetRule>();

                return instance;
            }
        }

        //Ported
        public ExtrapolationModel GetExtrapolationModel(string name)
        {
            lock (thisLock )
            {
                ExtrapolationModel instance =
                    Engine.Instance.Session.CreateCriteria<ExtrapolationModel>().Add(
                        Restrictions.Eq("Name", name))
                            .UniqueResult<ExtrapolationModel>();

                return instance;
            }
        }

        //Ported
        public ExtrapolationModel GetExtrapolationModel(int Id)
        {
            lock (thisLock )
            {
                ExtrapolationModel instance =
                    Engine.Instance.Session.CreateCriteria<ExtrapolationModel>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<ExtrapolationModel>();

                return instance;
            }
        }

        public MaturitySchedule GetMaturitySchedule(Classification c)
        {
            return GetMaturitySchedule(c.Id);
        }

        public MaturitySchedule GetMaturitySchedule(int classificationId)
        {
            lock (thisLock )
            {
                MaturitySchedule instance =
                    Engine.Instance.Session.CreateCriteria<MaturitySchedule>()
                    .Add(
                        Restrictions.Eq("ClassificationId", classificationId))
                            .UniqueResult<MaturitySchedule>();

                return instance;
            }
        }

        //Ported
        public Extrapolation GetLastExtrapolation(Underlying und)
        {
            return GetLastExtrapolation(und.Id);
        }

        //Ported
        public Extrapolation GetLastExtrapolation(int underlyingId)
        {
            lock (thisLock )
            {
                string sql =

                " select * from puma_mde_extrapolation where id = ( " +
                " select " +
                "      max(id) " +
                " from  " +
                "     puma_mde_extrapolation v " +
                " where  " +
                "      v.underlying_id= :underlying_id " +
                " and   v.timestamp= " +
                " ( " +
                "    select  " +
                "         max(timestamp) " +
                "    from  " +
                "         puma_mde_extrapolation v " +
                "    where  " +
                "          v.underlying_id= :underlying_id " +
                " ) " +
                ")";

                Extrapolation instance = null;


                try
                {
                    instance = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(Extrapolation))
                                .SetInt32("underlying_id", underlyingId)
                                        .UniqueResult<Extrapolation>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting extrapolation", e);
                }

                return instance;
            }
        }

        //Ported
        public Volsurface GetLastVolsurface(Underlying und)
        {
            return GetLastVolsurface(und.Id);
        }

        public YieldCurve GetLastYieldCurve(Underlying und, int ycCode)
        {
            return GetLastYieldCurve(und.Id, ycCode);
        }

        public YieldCurve GetLastYieldCurve(int underlyingId, int ycCode)
        {
            lock (thisLock )
            {
                string sql =

                " select * from puma_mde_yieldcurve where id = ( " +
                " select " +
                "      max(v.id) " +
                " from  " +
                "     puma_mde_yieldcurve v" +
                " where  " +
                "      v.underlying_id= :underlying_id " +
                " and v.yield_curve_code = :yc_code " +
                " and   v.timestamp= " +
                " ( " +
                "    select  " +
                "         max(timestamp) " +
                "    from  " +
                "         puma_mde_yieldcurve v " +
                "    where  " +
                "          v.underlying_id= :underlying_id " +
                " and v.yield_curve_code = :yc_code " +
                " ) " +
                ")";

                YieldCurve instance = null;

                try
                {
                    instance = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(YieldCurve))
                                .SetInt32("underlying_id", underlyingId)
                                    .SetInt32("yc_code", ycCode)
                                        .UniqueResult<YieldCurve>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("Error while getting YieldCurve", e);
                }

                return instance;
            }
        }

        //Ported
        public Volsurface GetLastVolsurface(int underlyingId)
        {
            lock (thisLock )
            {
                string sql =

                " select * from puma_mde_volsurface where id = ( " +
                " select " +
                "      max(v.id) " +
                " from  " +
                "     puma_mde_volsurface v, " +
                "     puma_mde_volsurface_model m " +
                " where  " +
                "      v.underlying_id= :underlying_id " +
                " and  v.volmodel_id = m.id" +
                " and  m.name!='" + "'" +
                " and   v.timestamp= " +
                " ( " +
                "    select  " +
                "         max(timestamp) " +
                "    from  " +
                "         puma_mde_volsurface v, " +
                "         puma_mde_volsurface_model m " +
                "    where  " +
                "          v.underlying_id= :underlying_id " +
                "    and  v.volmodel_id = m.id" +
                "    and  m.name!='" + "'" +
                " ) " +
                ")";

                Volsurface instance = null;


                try
                {
                    instance = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(Volsurface))
                                .SetInt32("underlying_id", underlyingId)
                                        .UniqueResult<Volsurface>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting volsurface", e);
                }

                return instance;
            }
        }


        public UsedMarketdata GetUsedMarketdata(int Id)
        {
            lock (thisLock )
            {
                UsedMarketdata UsedMarketdata =
                    Engine.Instance.Session.CreateCriteria<UsedMarketdata>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<UsedMarketdata>();

                return UsedMarketdata;
            }
        }

        //Ported
        public BenchmarkLink GetBenchmarkLinkForUnderlying(Underlying und)
        {
            return GetBenchmarkLinkForUnderlying(und.Id);
        }

        //Ported
        public BenchmarkLink GetBenchmarkLinkForUnderlying(int underlyingId)
        {
            lock (thisLock )
            {
                BenchmarkLink instance =
                    Engine.Instance.Session.CreateCriteria<BenchmarkLink>().Add(
                        Restrictions.Eq("UnderlyingId", underlyingId))
                            .UniqueResult<BenchmarkLink>();

                return instance;
            }
        }

        //Ported
        public BenchmarkLink GetBenchmarkLink(int Id)
        {
            lock (thisLock )
            {
                BenchmarkLink instance =
                    Engine.Instance.Session.CreateCriteria<BenchmarkLink>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<BenchmarkLink>();

                return instance;
            }
        }

        //Ported
        public VolMonitor GetVolMonitor(int Id)
        {
            lock (thisLock )
            {
                VolMonitor instance =
                    Engine.Instance.Session.CreateCriteria<VolMonitor>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<VolMonitor>();

                return instance;
            }
        }

        public VolMonitors GetVolMonitorsUnordered(string workset)
        {
            lock (thisLock)
            {
                IList<VolMonitor> instances;

                if (workset[0] == '>')
                {
                    instances = new List<VolMonitor>();
                    foreach (string undref in workset.Split(new char[] { '>', ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        foreach (VolMonitor mon in GetVolMonitorsForUnderlying(
                            GetUnderlying(undref)))
                        {
                            if (mon.Enabled)
                                instances.Add(mon);
                        }
                    }
                }
                else
                {
                    instances =
                        Engine.Instance.Session.CreateCriteria<VolMonitor>().Add(
                            Restrictions.Eq("Enabled", true)).Add(
                                Restrictions.Like("Workset", workset))
                                    .List<VolMonitor>();
                }

                Engine.Instance.Log.Debug("VolMonitors({0}):", workset);
                foreach (VolMonitor vm in instances)
                {
                    Engine.Instance.Log.Debug("Workset: {0} | Reference: {1}", vm.Workset, vm.Reference);
                }

                return new VolMonitors(instances);
            }
        }

        //Ported
        public VolMonitors GetVolMonitors(string workset)
        {
            var behaviourConfig = "unordered";
            
            if (behaviourConfig.Equals("unordered"))
            {
                Engine.Instance.Log.Info("Using non-ordered (previous) implementation of GetVolMonitors due to setting GetVolMonitorsOrder={0}", behaviourConfig);
                return GetVolMonitorsUnordered(workset);
            }

            var USCurrencies = string.Empty;
           
            var timeWindow = 0;
            
            string benchmarkUS = "AAPL.OQ";
            
            lock (thisLock)
            {
                IList<VolMonitor> instances;

                if (workset[0] == '>')
                {
                    instances = new List<VolMonitor>();
                    foreach (string undref in workset.Split(new char[] { '>', ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        foreach (VolMonitor mon in GetVolMonitorsForUnderlying(GetUnderlying(undref)))
                        {
                            if (mon.Enabled)
                                instances.Add(mon);
                        }
                    }
                }
                else
                {
                    List<string> currenciesToInclude = string.IsNullOrEmpty(USCurrencies) ? null : USCurrencies.Split(new char[] { ',' }).ToList();
                    instances = GetAllVolMonitorsOrdered(workset, timeWindow, 1, currenciesToInclude, benchmarkUS);
                }

                Engine.Instance.Log.Debug("VolMonitors({0}):", workset);
                foreach (VolMonitor vm in instances)
                {
                    Engine.Instance.Log.Debug("Workset: {0} | Reference: {1}", vm.Workset, vm.Reference);
                }

                return new VolMonitors(instances);
            }
        }

        //Ported
        public VolMonitors GetAllVolMonitors(string workset)
        {
            lock (thisLock )
            {
                IList<VolMonitor> instances =
                    Engine.Instance.Session.CreateCriteria<VolMonitor>().Add(
                            Restrictions.Like("Workset", workset))
                                .List<VolMonitor>();

                return new VolMonitors(instances);
            }
        }

        public IList<VolMonitor> GetAllVolMonitorsOrdered(string workset, int timeWindow, int warrantsThreshold, List<string> currenciesToInclude, string benchmarkUS)
        {
            lock (thisLock)
            {
                string sql = @"with priority_list as 
(
  select 
         count(pp.id) number_of_warrants, 
         tu.reference underlying, 
         devise_to_str(tu.devisectt) ccy
    from 
         puma_product        pp,
         titres              t,
         puma_product_data   ppd,
         panier              p,
         hv_pwp_table        x,
         titres              tu,
         puma_product_status pps,
         puma_productcombination ppc,
         puma_product_subtype    ppst,
         puma_product_type       ppt      
   where 
         1 = 1
     and ppd.product_id = pp.id
     and ppd.value_id = 106
     and ppd.value_number1 = t.sicovam
     and t.echeance > sysdate
     and p.sicovam = t.sicovam
     and x.code = p.sicopanier
     and tu.sicovam = x.hv_pwp_reference
     and pps.status_id = 200860
     and pps.product_id = pp.id 
     and pps.status_value_id not in
         (207306, 204230, 204229, 203366, 202280, 201721) 
     and pp.productcombinationid = ppc.id
     and ppst.id = ppc.product_subtype_id
     and ppst.name in ('Classic')
     and ppt.id = ppst.product_type_id
     and ppt.name = 'Warrant'
  group by 
        tu.reference, 
        tu.devisectt
),
monitors as 
(
    select * from puma_mde_volmonitor m where workset like :workset and enabled=1
),
timewindow as
(
    select m.START_TIME - trunc(m.START_TIME) as START_TIME, u.REFERENCE, m.ENABLED
    from puma_mde_volmonitor m, puma_mde_underlying u
    where m.underlying_id = u.id and u.REFERENCE = :benchmark_us and m.enabled=1
    order by m.START_TIME - trunc(m.START_TIME)
)
select 
     m.* 
from 
     priority_list p,
     monitors m,
     titres t,
     puma_mde_underlying u
where
     u.sophis_id     = t.sicovam (+)
and  p.underlying  (+)  = t.reference
and  m.underlying_id = u.id
and (
        t.type <> 'A' 
        or
        not exists(select * from timewindow)
        or
        m.run_once = 1
        or
        not ((select min(START_TIME) from timewindow) <= sysdate -  trunc(sysdate) and sysdate -  trunc(sysdate) <= (select min(START_TIME) from timewindow) + :timeWindow/1440)
        or
        (nvl(p.number_of_warrants, 0) > :warrantsThreshold and devise_to_str(t.devisectt) in (:currenciesToInclude))
     )
order by nvl(p.number_of_warrants, 0) desc
";
                
                IList<VolMonitor> instances = null;

                try
                {
                    instances = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(VolMonitor))
                                .SetString("workset", workset)
                                  .SetString("benchmark_us", benchmarkUS)
                                     .SetInt32("timeWindow", timeWindow)
                                        .SetInt32("warrantsThreshold", warrantsThreshold)
                                            .SetParameterList("currenciesToInclude", currenciesToInclude)
                                                .List<VolMonitor>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting volmonitors", e);
                }

                if (instances == null)
                    return null;

                return instances;
            }
        }

        public KxSuppliers  GetKxSuppliers(string workset)
        {
            lock (thisLock )
            {
                IList<KxSupply> instances =
                    Engine.Instance.Session.CreateCriteria<KxSupply>().Add(
                        Restrictions.Eq("Enabled", true)).Add(
                            Restrictions.Like("Workset", workset))
                                .List<KxSupply>();

                return new KxSuppliers(instances);
            }
        }

        //Ported
        public BenchmarkLinks GetDependentBenchmarkLinks(int benchmarkId)
        {
            lock (thisLock )
            {
                string sql =
    " select " +
    "     b.* " +
    " from  " +
    "     puma_mde_volsurface v, " +
    "     puma_mde_benchmark_link b, " +
    "     puma_mde_underlying u,      " +
    "     puma_mde_volsurface_model m, " +
    "     ( " +
    " select  " +
    "    u.id, max(u.reference), max(v.timestamp) timestamp " +
    " from  " +
    "     puma_mde_volsurface v,  " +
    "     puma_mde_underlying u,  " +
    "     puma_mde_volsurface_model m  " +
    " where  " +
    "    u.id=v.underlying_id  " +
    " and m.id = v.volmodel_id  " +
    " group by u.id " +
    "      ) x " +
    " where " +
    "      v.underlying_id = x.id " +
    " and   v.timestamp = x.timestamp " +
    " and   v.volmodel_id = m.id " +
    " and   m.name like 'SPREAD%' " +
    " and   b.underlying_id = v.underlying_id " +
    " and   b.benchmark_id = u.id " +
    " and   u.id = :benchmark "
                ;
                IList<BenchmarkLink> instances =
                    Engine.Instance.Session.CreateSQLQuery(sql)
                        .AddEntity(typeof(BenchmarkLink))
                        .SetInt32("benchmark", benchmarkId).List<BenchmarkLink>();

                //IList<BenchmarkLink> instances =
                //    Engine.Instance.Session.CreateCriteria<BenchmarkLink>().Add(
                //        Restrictions.Eq("BenchmarkId", benchmarkId))
                //            .List<BenchmarkLink>();

                return new BenchmarkLinks(instances);
            }
        }

        //Ported
        public BenchmarkLinks GetDependentBenchmarkLinks(Underlying benchmark)
        {
            return GetDependentBenchmarkLinks(benchmark.Id);
        }

        //Ported
        public Underlying GetUnderlyingByRIC(string ric)
        {
            lock (thisLock )
            {
                Underlying underlying =
                    Engine.Instance.Session.CreateCriteria<Underlying>().Add(
                        Restrictions.Eq("RIC", ric))
                            .AddOrder(Order.Asc("Id"))
                                .SetMaxResults(1)
                                    .UniqueResult<Underlying>();

                return underlying;
            }
        }

        //Ported
        public ExtrapolationModels GetExtrapolationModels()
        {
            lock (thisLock )
            {
                IList<ExtrapolationModel> instances =
                    Engine.Instance.Session.CreateCriteria<ExtrapolationModel>()
                                .List<ExtrapolationModel>();

                return new ExtrapolationModels(instances);
            }
        }

        //Ported
        [ComVisible(false)]
        public IList<QuixReweightModel> GetQuixReweightModels()
        {
            lock (thisLock )
            {
                IList<QuixReweightModel> instances =
                    Engine.Instance.Session.CreateCriteria<QuixReweightModel>()
                                .List<QuixReweightModel>();

                return instances;
            }
        }

        //Ported
        public QuixReweightModel GetQuixReweightModel(string name)
        {
            lock (thisLock )
            {
                QuixReweightModel instance =
                    Engine.Instance.Session.CreateCriteria<QuixReweightModel>().Add(
                        Restrictions.Eq("Name", name))
                            .UniqueResult<QuixReweightModel>();

                return instance;
            }
        }

        //Ported
        public QuixReweightModel GetQuixReweightModel(int id)
        {
            lock (thisLock )
            {
                QuixReweightModel instance =
                    Engine.Instance.Session.CreateCriteria<QuixReweightModel>().Add(
                        Restrictions.Eq("Id", id))
                            .UniqueResult<QuixReweightModel>();

                return instance;
            }
        }

        //Ported
        public QuixReweight GetLastQuixReweight(Underlying und)
        {
            return GetLastReweight(und.Id);
        }

        //Ported
        public QuixReweight GetLastReweight(int underlyingId)
        {
            lock (thisLock )
            {
                string sql =

                " select * from PUMA_MDE_QUIX_REWEIGHT where id = ( " +
                " select " +
                "      max(id) " +
                " from  " +
                "     PUMA_MDE_QUIX_REWEIGHT v " +
                " where  " +
                "      v.underlying_id= :underlying_id " +
                " and   v.timestamp= " +
                " ( " +
                "    select  " +
                "         max(timestamp) " +
                "    from  " +
                "         PUMA_MDE_QUIX_REWEIGHT v " +
                "    where  " +
                "          v.underlying_id= :underlying_id " +
                " ) " +
                ")";

                QuixReweight instance = null;


                try
                {
                    instance = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(QuixReweight))
                                .SetInt32("underlying_id", underlyingId)
                                        .UniqueResult<QuixReweight>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting extrapolation", e);
                }

                return instance;
            }
        }

        //Ported
        public VolMonitors GetVolMonitorsForUnderlying(Underlying und)
        {
            lock (thisLock )
            {
                IList<VolMonitor> instances =
                    Engine.Instance.Session.CreateCriteria<VolMonitor>().Add(
                        Restrictions.Eq("UnderlyingId", und.Id))
                            .List<VolMonitor>();

                return new VolMonitors(instances);
            }
        }

        //Ported
        public Volsurface GetVolsurfaceAt(Underlying und, DateTime Timestamp)
        {
            return GetVolsurfaceAt(und.Id, Timestamp);
        }

        //Ported
        public Volsurface GetVolsurfaceAt(int underlyingId, DateTime timestamp)
        {
            lock (thisLock )
            {
                string sql =

                " select * from puma_mde_volsurface where id = ( " +
                " select " +
                "      max(v.id) " +
                " from  " +
                "     puma_mde_volsurface       v, " +
                "     puma_mde_volsurface_model m " +
                " where  " +
                "       v.underlying_id= :underlying_id " +
                " and   v.volmodel_id=m.id " +
                " and   m.name!='" + "' " +
                " and   v.timestamp= " +
                " ( " +
                "    select  " +
                "         max(timestamp) " +
                "    from  " +
                "         puma_mde_volsurface v, " +
                "         puma_mde_volsurface_model m " +
                "    where  " +
                "          v.underlying_id= :underlying_id " +
                "    and   v.volmodel_id=m.id " +
                "    and   m.name!='" + "' " +
                "    and   v.timestamp< :timestamp " +
                " ) " +
                ")";

                Volsurface instance = null;


                try
                {
                    instance = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(Volsurface))
                                .SetInt32("underlying_id", underlyingId)
                                .SetDateTime("timestamp", timestamp)
                                        .UniqueResult<Volsurface>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting volsurface", e);
                }

                return instance;
            }
        }

        public PreAfterMarketAdjustmentMonitor GetPreAfterMarketAdjustmentMonitor(int Id)
        {
            lock (thisLock )
            {
                PreAfterMarketAdjustmentMonitor instance =
                    Engine.Instance.Session.CreateCriteria<PreAfterMarketAdjustmentMonitor>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<PreAfterMarketAdjustmentMonitor>();

                return instance;
            }
        }

        public PreAfterMarketAdjustmentMonitors GetPreAfterMarketAdjustmentMonitors(string workset)
        {
            lock (thisLock )
            {
                IList<PreAfterMarketAdjustmentMonitor> instances =
                    Engine.Instance.Session.CreateCriteria<PreAfterMarketAdjustmentMonitor>().Add(
                        Restrictions.Eq("Enabled", true)).Add(
                            Restrictions.Like("Workset", workset))
                                .List<PreAfterMarketAdjustmentMonitor>();

                return new PreAfterMarketAdjustmentMonitors(instances);
            }
        }

        public PreAfterMarketAdjustmentMonitors GetAllPreAfterMarketAdjustmentMonitors(string workset)
        {
            lock (thisLock )
            {
                IList<PreAfterMarketAdjustmentMonitor> instances =
                    Engine.Instance.Session.CreateCriteria<PreAfterMarketAdjustmentMonitor>().Add(
                            Restrictions.Like("Workset", workset))
                                .List<PreAfterMarketAdjustmentMonitor>();

                return new PreAfterMarketAdjustmentMonitors(instances);
            }
        }

        public PreAfterMarketAdjustmentMonitors GetPreAfterMarketAdjustmentMonitorsForUnderlying(Underlying und)
        {
            lock (thisLock )
            {
                IList<PreAfterMarketAdjustmentMonitor> instances =
                    Engine.Instance.Session.CreateCriteria<PreAfterMarketAdjustmentMonitor>().Add(
                        Restrictions.Eq("UnderlyingId", und.Id))
                            .List<PreAfterMarketAdjustmentMonitor>();

                return new PreAfterMarketAdjustmentMonitors(instances);
            }
        }

        public YieldCurveMonitor GetYieldCurveMonitor(int Id)
        {
            lock (thisLock )
            {
                YieldCurveMonitor instance =
                    Engine.Instance.Session.CreateCriteria<YieldCurveMonitor>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<YieldCurveMonitor>();

                return instance;
            }
        }

        public YieldCurveMonitors GetYieldCurveMonitors(string workset)
        {
            lock (thisLock )
            {
                IList<YieldCurveMonitor> instances =
                    Engine.Instance.Session.CreateCriteria<YieldCurveMonitor>().Add(
                        Restrictions.Eq("Enabled", true)).Add(
                            Restrictions.Like("Workset", workset))
                                .List<YieldCurveMonitor>();

                return new YieldCurveMonitors(instances);
            }
        }

        [ComVisible(false)]
        public YieldCurveMonitors GetYieldCurveMonitors(ISession session, string workset)
        {
            //lock (thisLock)
            {
                IList<YieldCurveMonitor> instances =
                    session.CreateCriteria<YieldCurveMonitor>().Add(
                        Restrictions.Eq("Enabled", true)).Add(
                            Restrictions.Like("Workset", workset))
                                .List<YieldCurveMonitor>();

                return new YieldCurveMonitors(instances);
            }
        }

        public YieldCurveMonitors GetAllYieldCurveMonitors(string workset)
        {
            lock (thisLock )
            {
                IList<YieldCurveMonitor> instances =
                    Engine.Instance.Session.CreateCriteria<YieldCurveMonitor>().Add(
                            Restrictions.Like("Workset", workset))
                                .List<YieldCurveMonitor>();

                return new YieldCurveMonitors(instances);
            }
        }

        public YieldCurveMonitors GetYieldCurveMonitorsForUnderlying(Underlying und)
        {
            lock (thisLock )
            {
                IList<YieldCurveMonitor> instances =
                    Engine.Instance.Session.CreateCriteria<YieldCurveMonitor>().Add(
                        Restrictions.Eq("UnderlyingId", und.Id))
                            .List<YieldCurveMonitor>();

                return new YieldCurveMonitors(instances);
            }
        }

        //Ported
        public Volsurface GetLastVolsurfaceForVolmodel(Underlying und, VolsurfaceModel model)
        {
            return GetVolsurfaceForVolmodelAt(und, model, DateTime.Now);
        }

        //Ported
        public Volsurface GetVolsurfaceForVolmodelAt(Underlying und, VolsurfaceModel model, DateTime timestamp)
        {
            return GetVolsurfaceForVolmodelAt(und.Id, model.Id, timestamp);
        }

        //Ported
        public Volsurface GetVolsurfaceForVolmodelAt(int underlyingId, int volmodelId, DateTime timestamp)
        {
            lock (thisLock )
            {
                string sql =

                " select * from puma_mde_volsurface where id = ( " +
                " select " +
                "      max(id) " +
                " from  " +
                "     puma_mde_volsurface v " +
                " where  " +
                "      v.underlying_id= :underlying_id " +
                " and  v.volmodel_id= :volmodel_id " +
                " and  v.timestamp= " +
                " ( " +
                "    select  " +
                "         max(timestamp) " +
                "    from  " +
                "         puma_mde_volsurface v " +
                "    where  " +
                "          v.underlying_id= :underlying_id " +
                "    and   v.volmodel_id= :volmodel_id " +
                "    and   v.timestamp< :timestamp " +
                " ) " +
                ")";

                Volsurface instance = null;


                try
                {
                    instance = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(Volsurface))
                                .SetInt32("underlying_id", underlyingId)
                                .SetInt32("volmodel_id", volmodelId)
                                .SetDateTime("timestamp", timestamp)
                                        .UniqueResult<Volsurface>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting volsurface", e);
                }

                return instance;
            }
        }

        //Ported
        public Volsurfaces GetLastVolsurfacesForVolmodel(VolsurfaceModel model)
        {
            return GetLastVolsurfacesForVolmodel(model.Id);
        }

        //Ported
        public Volsurfaces GetLastVolsurfacesForVolmodel(int volmodelId)
        {
            lock (thisLock )
            {
                string sql =

                    " select * from puma_mde_volsurface where volmodel_id=:volmodel_id and (underlying_id, timestamp) in  " +
                    " (select underlying_id, max(timestamp) from puma_mde_volsurface where volmodel_id=:volmodel_id group by underlying_id) ";

                IList<Volsurface> instances = null;


                try
                {
                    instances = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(Volsurface))
                                .SetInt32("volmodel_id", volmodelId)
                                        .List<Volsurface>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting volsurfaces", e);
                }

                if (instances == null)
                    return null;

                return new Volsurfaces(instances);
            }
        }

        //Ported
        public Underlyings GetUnderlyingsLike(string like, int maxOfResults)
        {
            lock (thisLock )
            {
                IList<Underlying> instances =
                    Engine.Instance.Session.CreateCriteria<Underlying>()
                        .Add(
                            Restrictions.InsensitiveLike("Reference", "%" + like + "%"))
                                .SetMaxResults(maxOfResults)
                                    .List<Underlying>();

                return new Underlyings(instances);
            }
        }

        //Ported
        public VolMonitors GetLimitedVolMonitors(string like, int maxOfResults)
        {
            lock (thisLock )
            {
                string sql =
                    " select * from puma_mde_volmonitor where id in (select m.id from puma_mde_volmonitor m, puma_mde_underlying u where u.id=m.underlying_id and (m.workset like :likeexp or u.reference like :likeexp)) ";

                IList<VolMonitor> instances = null;


                try
                {
                    instances = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(VolMonitor))
                                .SetString("likeexp", like)
                                    .SetMaxResults(maxOfResults)
                                           .List<VolMonitor>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting volmonitors", e);
                }

                if (instances == null)
                    return null;

                return new VolMonitors(instances);
            }
        }

        public PddaMonitors GetLimitedPddaMonitors(string like, int maxOfResults)
        {
            lock (thisLock )
            {
                string sql =
                    " select * from puma_mde_Pddamonitor where id in (select m.id from puma_mde_Pddamonitor m, puma_mde_underlying u where u.id=m.underlying_id and (m.workset like :likeexp or u.reference like :likeexp)) ";

                IList<PddaMonitor> instances = null;


                try
                {
                    instances = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(PddaMonitor))
                                .SetString("likeexp", like)
                                    .SetMaxResults(maxOfResults)
                                           .List<PddaMonitor>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting Pddamonitors", e);
                }

                if (instances == null)
                    return null;

                return new PddaMonitors(instances);
            }
        }

        public YieldCurveMonitors GetLimitedYieldCurveMonitors(string like, int maxOfResults)
        {
            lock (thisLock )
            {
                string sql =
                    " select * from puma_mde_ycmonitor where id in (select m.id from puma_mde_ycmonitor m, puma_mde_underlying u where u.id=m.underlying_id and (m.workset like :likeexp or u.reference like :likeexp)) ";

                IList<YieldCurveMonitor> instances = null;


                try
                {
                    instances = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(YieldCurveMonitor))
                                .SetString("likeexp", like)
                                    .SetMaxResults(maxOfResults)
                                           .List<YieldCurveMonitor>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting ycmonitors", e);
                }

                if (instances == null)
                    return null;

                return new YieldCurveMonitors(instances);
            }
        }

        //Ported
        public ForwardMonitors GetLimitedForwardMonitors(string like, int maxOfResults)
        {
            lock (thisLock )
            {
                string sql =
                    " select * from puma_mde_fwdmonitor where id in (select m.id from puma_mde_fwdmonitor m, puma_mde_underlying u where u.id=m.underlying_id and (m.workset like :likeexp or u.reference like :likeexp)) ";

                IList<ForwardMonitor> instances = null;


                try
                {
                    instances = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(ForwardMonitor))
                                .SetString("likeexp", like)
                                    .SetMaxResults(maxOfResults)
                                           .List<ForwardMonitor>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting Fwdmonitors", e);
                }

                if (instances == null)
                    return null;

                return new ForwardMonitors(instances);
            }
        }

        //Ported
        public ForwardMonitor GetForwardMonitor(int Id)
        {
            lock (thisLock )
            {
                ForwardMonitor instance =
                    Engine.Instance.Session.CreateCriteria<ForwardMonitor>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<ForwardMonitor>();

                return instance;
            }
        }

        //Ported
        public ForwardMonitors GetForwardMonitors(string workset)
        {
            lock (thisLock )
            {
                IList<ForwardMonitor> instances =
                    Engine.Instance.Session.CreateCriteria<ForwardMonitor>().Add(
                        Restrictions.Eq("Enabled", true)).Add(
                            Restrictions.Like("Workset", workset))
                                .List<ForwardMonitor>();

                return new ForwardMonitors(instances);
            }
        }

        //Ported
        public ForwardMonitors GetAllForwardMonitors(string workset)
        {
            lock (thisLock )
            {
                IList<ForwardMonitor> instances =
                    Engine.Instance.Session.CreateCriteria<ForwardMonitor>().Add(
                            Restrictions.Like("Workset", workset))
                                .List<ForwardMonitor>();

                return new ForwardMonitors(instances);
            }
        }

        //Ported
        public ForwardMonitors GetForwardMonitorsForUnderlying(Underlying und)
        {
            lock (thisLock )
            {
                IList<ForwardMonitor> instances =
                    Engine.Instance.Session.CreateCriteria<ForwardMonitor>().Add(
                        Restrictions.Eq("UnderlyingId", und.Id))
                            .List<ForwardMonitor>();

                return new ForwardMonitors(instances);
            }
        }

        public static T Clone<T>(T source)
        {
            lock (source)
            {
                T dest = default(T);

                using (var stream = new System.IO.MemoryStream())
                {
                    var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                    formatter.Serialize(stream, source);
                    stream.Position = 0;
                    dest = (T)formatter.Deserialize(stream);

                }

                return dest;
            }
        }

        [ComVisible(false)]
        public VolMonitors GetAlarmedLimitedVolMonitorsRegExp(ISession session, string like, int maxOfResults, int minOfSilence)
        {
            //lock (thisLock )
            {
                //string sql =
                //    " select * from puma_mde_volmonitor where enabled!=0 and id in " +
                //    " (select m.id from puma_mde_volmonitor m, puma_mde_underlying u where u.id=m.underlying_id and (m.workset like :likeexp or u.reference like :likeexp) and " +
                //    " (m.volatility_alarm<>0 or m.repo_alarm<>0 or m.status_description<>'' or (run_once=0 and published_at<(sysdate-:minofsilence / 24 / 60) ) ) ) ";

                string sql =
                    "select a.* from puma_mde_volmonitor a, " +
                    " (select a.underlying_id, max(checked_at) as ts from puma_mde_volmonitor a where volatility_alarm = 1 group by underlying_id) b " +
                    " where a.enabled!=0 and a.id in " +
                    " (select m.id from puma_mde_volmonitor m, puma_mde_underlying u where u.id=m.underlying_id and (regexp_like(upper(m.workset),upper(:likeexp)) or regexp_like(upper(u.reference),upper(:likeexp))) and  " +
                    " (m.volatility_alarm<>0 or m.repo_alarm<>0 or m.status_description<>'' or (run_once=0 and published_at<(sysdate-:minofsilence / 24 / 60) ) ) ) and  a.underlying_id=b.underlying_id and a.checked_at= b.ts ";

                IList<VolMonitor> instances = null;


                try
                {
                    instances = 
                        session.CreateSQLQuery(sql)
                            .AddEntity(typeof(VolMonitor))
                                .SetString("likeexp", like)
                                    .SetInt32("minofsilence", minOfSilence)
                                        .SetMaxResults(maxOfResults)
                                           .List<VolMonitor>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting volmonitors", e);
                }

                if (instances == null)
                    return null;

                return new VolMonitors(instances);
            }
        }


        //Ported
        [ComVisible(false)]
        public VolMonitors GetAlarmedLimitedVolMonitors(ISession session, string like, int maxOfResults, int minOfSilence)
        {
            //lock (thisLock )
            {
                //string sql =
                //    " select * from puma_mde_volmonitor where enabled!=0 and id in " +
                //    " (select m.id from puma_mde_volmonitor m, puma_mde_underlying u where u.id=m.underlying_id and (m.workset like :likeexp or u.reference like :likeexp) and " +
                //    " (m.volatility_alarm<>0 or m.repo_alarm<>0 or m.status_description<>'' or (run_once=0 and published_at<(sysdate-:minofsilence / 24 / 60) ) ) ) ";

                string sql =
                    "select a.* from puma_mde_volmonitor a, " +
                    " (select a.underlying_id, max(checked_at) as ts from puma_mde_volmonitor a where volatility_alarm = 1 group by underlying_id) b " + 
                    " where a.enabled!=0 and a.id in " +
                    " (select m.id from puma_mde_volmonitor m, puma_mde_underlying u where u.id=m.underlying_id and (m.workset like :likeexp or u.reference like :likeexp) and  " +
                    " (m.volatility_alarm<>0 or m.repo_alarm<>0 or m.status_description<>'' or (run_once=0 and published_at<(sysdate-:minofsilence / 24 / 60) ) ) ) and  a.underlying_id=b.underlying_id and a.checked_at= b.ts ";

                IList<VolMonitor> instances = null;


                try
                {
                    instances = 
                        session.CreateSQLQuery(sql)
                            .AddEntity(typeof(VolMonitor))
                                .SetString("likeexp", like)
                                    .SetInt32("minofsilence", minOfSilence)
                                        .SetMaxResults(maxOfResults)
                                           .List<VolMonitor>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting volmonitors", e);
                }

                if (instances == null)
                    return null;

                return new VolMonitors(instances);
            }
        }

        //Ported
        public Volsurface GetLastVolsurfaceBefore(int underlyingId)
        {
            lock (thisLock )
            {
                string sql =

                " select * from puma_mde_volsurface where id in ( " +

                    " select id from  " +
                    " ( " +
                    "  select   " +
                    "       v.id " +
                    "  from    " +
                    "      puma_mde_volsurface v,   " +
                    "      puma_mde_volsurface_model m   " +
                    "  where    " +
                    "       v.underlying_id=:underlying_id " +
                    "  and  v.volmodel_id = m.id  " +
                    "  and  m.name!='" + "'  " +
                    "  order by id desc " +
                    "  ) " +
                    "   " +
                    "  where rownum<=2 " +

                ")";

                Volsurface instance = null;


                try
                {
                    IList<Volsurface> instances = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(Volsurface))
                                .SetInt32("underlying_id", underlyingId)
                                        .List<Volsurface>();

                    if (instances.Count > 1)
                        instance = instances[1];

                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting volsurface", e);
                }

                return instance;
            }
        }

        //Ported
        public Volsurface GetLastVolsurfaceBefore(Underlying und)
        {
            return GetLastVolsurfaceBefore(und.Id);
        }

        public PublishingSchedules GetPublishingSchedules()
        {
            lock (thisLock )
            {
                IList<PublishingSchedule> instances =
                    Engine.Instance.Session.CreateCriteria<PublishingSchedule>()
                                .List<PublishingSchedule>();

                return new PublishingSchedules(instances);
            }
        }

        public PublishingSchedule GetPublishingSchedule(int Id)
        {
            lock (thisLock )
            {
                PublishingSchedule PublishingSchedule =
                    Engine.Instance.Session.CreateCriteria<PublishingSchedule>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<PublishingSchedule>();

                return PublishingSchedule;
            }
        }

        public PublishingSchedule GetPublishingSchedule(string name)
        {
            lock (thisLock )
            {
                PublishingSchedule PublishingSchedule =
                    Engine.Instance.Session.CreateCriteria<PublishingSchedule>().Add(
                        Restrictions.Eq("Name", name))
                            .UniqueResult<PublishingSchedule>();

                return PublishingSchedule;
            }
        }

        public Int32 GetSophisIDFromReference(string Reference)
        {
            lock (thisLock )
            {
                Decimal retval = 0;
                try
                {
                    retval = Engine.Instance.
                        Session.CreateSQLQuery("select sicovam from titres where reference=:reference")
                               .SetString("reference", Reference)
                                        .UniqueResult<Decimal>();

                    return (Int32)retval;
                }
                catch (Exception)
                {
                }

                return (Int32)retval;
            }
        }

        public string GetSophisTypeFromReference(string Reference)
        {
            lock (thisLock)
            {
                string retval = null;

                try
                {
                    retval = Engine.Instance.
                        Session.CreateSQLQuery("select type from titres where reference=:reference")
                               .SetString("reference", Reference)
                                        .UniqueResult<string>();

                    return retval;
                }
                catch (Exception)
                {
                }

                return retval;
            }
        }

        public Int32 GetCommodityFutureSophisId(Int32 commodity, Int32 expiryInDays)
        {
            lock (thisLock )
            {
                Decimal retval = 0;

                try
                {
                    retval = Engine.Instance.
                        Session.CreateSQLQuery("select sicovam from titres t where t.type='F' and t.code_emet=:underlying and  date_to_num(t.echeance) in (select min(date_to_num(echeance)) from titres where type='F' and code_emet=:underlying2 and date_to_num(echeance)>=:expiryInDays)")
                               .SetInt32("underlying", commodity)
                               .SetInt32("underlying2", commodity)
                               .SetInt32("expiryInDays", expiryInDays)
                               .UniqueResult<Decimal>();

                    return (Int32)retval;
                }
                catch (Exception)
                {
                }

                return (Int32)retval;

            }
        }
        
        public String GetSophisReferenceFromId(Int32 id)
        {
            lock (thisLock )
            {
                String retval = Engine.Instance.
                    Session.CreateSQLQuery("select reference from titres where sicovam=:id")
                           .SetInt32("id", id)
                                    .UniqueResult<String>();

                return retval;
            }
        }

        //Ported
        public Underlying GetUnderlyingFromSophisId(int SophisId)
        {
            lock (thisLock )
            {
                Underlying underlying =
                    Engine.Instance.Session.CreateCriteria<Underlying>().Add(
                        Restrictions.Eq("SophisId", SophisId))
                            .UniqueResult<Underlying>();

                return underlying;
            }
        }

        //Ported
        [ComVisible(false)]
        public VolMonitors GetAlarmedLimitedVolMonitorsByClassification(ISession session, string like, int maxOfResults, int minOfSilence)
        {
            //lock (thisLock )
            {
                string sql =
                    @" select a.* from puma_mde_volmonitor a,  
                     (select a.underlying_id, max(checked_at) as ts from puma_mde_volmonitor a where volatility_alarm = 1 group by underlying_id) b  
                     where a.enabled!=0 and a.id in  
                     (select m.id from puma_mde_volmonitor m, puma_mde_underlying u where u.id=m.underlying_id and 
                     underlying_id in ( select underlying_id from puma_mde_underlying_class where classification_id in (select id from puma_mde_classification where name like :likeexp))
                     and   
                     (m.volatility_alarm<>0 or m.repo_alarm<>0 or m.status_description<>'' or (run_once=0 and published_at<(sysdate-:minofsilence / 24 / 60) ) ) ) and  a.underlying_id=b.underlying_id and a.checked_at= b.ts ";

                IList<VolMonitor> instances = null;


                try
                {
                    instances = 
                        session.CreateSQLQuery(sql)
                            .AddEntity(typeof(VolMonitor))
                                .SetString("likeexp", like)
                                    .SetInt32("minofsilence", minOfSilence)
                                        .SetMaxResults(maxOfResults)
                                           .List<VolMonitor>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting volmonitors", e);
                }

                if (instances == null)
                    return null;

                return new VolMonitors(instances);
            }
        }

        [ComVisible(false)]
        public VolMonitors GetAlarmedLimitedVolMonitorsByClassificationRegExp(ISession session, string like, int maxOfResults, int minOfSilence)
        {
            //lock (thisLock)
            {
                string sql =
                @" select a.* from puma_mde_volmonitor a,  
                 (select a.underlying_id, max(checked_at) as ts from puma_mde_volmonitor a where volatility_alarm = 1 group by underlying_id) b  
                 where a.enabled!=0 and a.id in  
                 (select m.id from puma_mde_volmonitor m, puma_mde_underlying u where u.id=m.underlying_id and 
                 underlying_id in ( select underlying_id from puma_mde_underlying_class where classification_id in (select id from puma_mde_classification where regexp_like(upper(name),upper(:likeexp))))
                 and   
                 (m.volatility_alarm<>0 or m.repo_alarm<>0 or m.status_description<>'' or (run_once=0 and published_at<(sysdate-:minofsilence / 24 / 60) ) ) ) and  a.underlying_id=b.underlying_id and a.checked_at= b.ts ";

                IList<VolMonitor> instances = null;


                try
                {
                    instances = 
                        session.CreateSQLQuery(sql)
                            .AddEntity(typeof(VolMonitor))
                                .SetString("likeexp", like)
                                    .SetInt32("minofsilence", minOfSilence)
                                        .SetMaxResults(maxOfResults)
                                           .List<VolMonitor>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting volmonitors", e);
                }

                if (instances == null)
                    return null;

                return new VolMonitors(instances);
            }
        }

        //Ported
        public DateTime GetLastVolsurfaceTimeStamp(Underlying und)
        {
            lock (thisLock )
            {
                string sql =
                    " select max(timestamp) from puma_mde_volsurface where underlying_id = :id";
                DateTime instance = new DateTime();

                try
                {
                    instance = Engine.Instance.Session.CreateSQLQuery(sql)
                        .SetString("id", und.Id.ToString())
                            .UniqueResult<DateTime>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting max vol time stamp", e);
                }
                return instance;
            }
        }

        //Ported
        public Classifications GetClassifications()
        {
            lock (thisLock )
            {
                IList<Classification> instances =
                    Engine.Instance.Session.CreateCriteria<Classification>()
                                .List<Classification>();

                return new Classifications(instances);
            }
        }

        public WorksetStatusEnum GetWorksetStatus(string workset)
        {
            lock (thisLock )
            {
                WorksetStatusEnum retval = WorksetStatusEnum.Regular;

                Int32 counttotal = 0;

                try
                {
                    string sql =
                        " select count(*) from puma_mde_volmonitor where (workset like :workset or (underlying_id in ( select underlying_id from puma_mde_underlying_class where classification_id in (select id from puma_mde_classification where name like :workset))))";
                    counttotal = (Int32)Engine.Instance.Session.CreateSQLQuery(sql)
                        .SetString("workset", workset)
                            .UniqueResult<Decimal>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting workset status", e);
                }

                Int32 countdisabled = 0;

                try
                {
                    string sql =
                        " select count(*) from puma_mde_volmonitor where (workset like :workset or (underlying_id in ( select underlying_id from puma_mde_underlying_class where classification_id in (select id from puma_mde_classification where name like :workset)))) and enabled=0";
                    countdisabled = (Int32)Engine.Instance.Session.CreateSQLQuery(sql)
                        .SetString("workset", workset)
                            .UniqueResult<Decimal>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting workset status", e);
                }

                Int32 countATMonly = 0;

                try
                {
                    string sql =
                        " select count(*) from puma_mde_volmonitor where (workset like :workset or (underlying_id in ( select underlying_id from puma_mde_underlying_class where classification_id in (select id from puma_mde_classification where name like :workset)))) and completepublish=0";
                    countATMonly = (Int32)Engine.Instance.Session.CreateSQLQuery(sql)
                        .SetString("workset", workset)
                            .UniqueResult<Decimal>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting workset status", e);
                }


                if (counttotal > 0)
                {
                    if (countdisabled == counttotal)
                        retval = WorksetStatusEnum.Disabled;
                    else
                    {
                        if (countATMonly == counttotal)
                            retval = WorksetStatusEnum.ATMOnly;
                    }
                }
                return retval;
            }
        }

        //Ported
        public VolMonitors GetAllVolMonitorsByWorksetOrClassification(string like)
        {
            lock (thisLock )
            {
                string sql =
                    " select * from puma_mde_volmonitor where workset like :like or underlying_id in ( select underlying_id from puma_mde_underlying_class where classification_id in (select id from puma_mde_classification where name like :like))";

                IList<VolMonitor> instances = null;


                try
                {
                    instances = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(VolMonitor))
                                .SetString("like", like)
                                           .List<VolMonitor>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting volmonitors", e);
                }

                if (instances == null)
                    return null;

                return new VolMonitors(instances);
            }
        }

        [ComVisible(false)]
        public IList<PermissionType> GetPermissionTypes()
        {
            lock (thisLock )
            {
                IList<PermissionType> instances = null;
                try
                {
                    instances = Engine.Instance.
                        Session.CreateCriteria<PermissionType>().
                               List<PermissionType>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting permissions", e);
                }
                return instances;
            }
        }

        [ComVisible(false)]
        public IList<UserGroup> GetUserGroups()
        {
            lock (thisLock )
            {
                IList<UserGroup> instances = null;
                try
                {
                    instances = Engine.Instance.
                        Session.CreateCriteria<UserGroup>().
                               List<UserGroup>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting user groups", e);
                }
                return instances;
            }
        }

        
        [ComVisible(false)]
        public IList<UserGroupConnection> GetUserGroupConnections(int userGroupId)
        {
            lock (thisLock )
            {
                IList<UserGroupConnection> instances = null;
                try
                {
                    instances = Engine.Instance.
                        Session.CreateCriteria<UserGroupConnection>().
                            Add(Restrictions.Eq("GroupId", userGroupId)).
                               List<UserGroupConnection>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting user groups", e);
                }
                return instances;
            }
        }

        [ComVisible(false)]
        public IList<UserGroupConnection> GetUserGroupConnections(UserGroup group)
        {
            return GetUserGroupConnections(group.Id) ;
        }

        [ComVisible(false)]
        public IList<UserRight> GetUserRightsForClassification(int classificationId)
        {
            lock (thisLock )
            {
                IList<UserRight> instances = null;
                try
                {
                    instances = Engine.Instance.
                        Session.CreateCriteria<UserRight>().
                            Add(Restrictions.Eq("ClassificationId", classificationId)).
                               List<UserRight>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting user rights", e);
                }
                return instances;
            }
        }

        [ComVisible(false)]
        public IList<UserRight> GetUserRightsForClassification(Classification cla)
        {
            return GetUserRightsForClassification(cla.Id);
        }

        [ComVisible(false)]
        public IList<UserRight> GetUserRightsForUserGroup(int userGroupId)
        {
            lock (thisLock )
            {

                IList<UserRight> instances = null;
                try
                {
                    instances = Engine.Instance.
                        Session.CreateCriteria<UserRight>().
                            Add(Restrictions.Eq("GroupId", userGroupId)).
                               List<UserRight>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting user rights", e);
                }
                return instances;
            }
        }

        [ComVisible(false)]
        public IList<UserRight> GetUserRightsForUserGroup(UserGroup group)
        {
            return GetUserRightsForUserGroup(group.Id);
        }

        [ComVisible(false)]
        public UserGroupConnection GetUserGroupConnection(int userId, int groupId)
        {
            lock (thisLock )
            {
                return Engine.Instance.
                    Session.CreateCriteria<UserGroupConnection>().
                        Add(Restrictions.Eq("UserId", userId)).
                            Add(Restrictions.Eq("GroupId", groupId)).
                               UniqueResult<UserGroupConnection>();
            }
        }

        [ComVisible(false)]
        public UserGroupConnection GetUserGroupConnection(User user, UserGroup group)
        {
            return GetUserGroupConnection(user.Id, group.Id);
        }

        [ComVisible(false)]
        public UserGroup GetUserGroup(int id)
        {
            lock (thisLock )
            {
                return Engine.Instance.
                    Session.CreateCriteria<UserGroup>().
                            Add(Restrictions.Eq("Id", id)).
                               UniqueResult<UserGroup>();
            }
        }

        [ComVisible(false)]
        public IList<UserGroup> GetUserGroups(int userId)
        {
            IList<UserGroupConnection> groupConnections = null;
            IList<UserGroup> userGroups = new List<UserGroup>();

            lock (thisLock )
            {
                try
                {
                    groupConnections = Engine.Instance.
                        Session.CreateCriteria<UserGroupConnection>().
                            Add(Restrictions.Eq("UserId", userId)).
                               List<UserGroupConnection>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting user groups", e);
                }

                if (groupConnections == null)
                {
                    Engine.Instance.ErrorException("error while getting user groups", 
                                                       new NullReferenceException("No UserGroupConnection exists for this user."));
                    return null;
                }
            }

            try
            {
                foreach(var groupConnection in groupConnections)
                {
                    var id = groupConnection.GroupId;
                    var userGroup = GetUserGroup(id);

                    if (userGroup != null)
                        userGroups.Add(userGroup);
                }
            }
            catch (Exception e)
            {
                Engine.Instance.ErrorException("error while getting user group for a particular user group connection.", e);
            }

            return userGroups;
        }


        [ComVisible(false)]
        public PermissionType GetPermissionType(int id)
        {
            lock (thisLock )
            {
                return Engine.Instance.
                    Session.CreateCriteria<PermissionType>().
                            Add(Restrictions.Eq("Id", id)).
                               UniqueResult<PermissionType>();
            }
        }

        //Ported
        public Volsurface GetVolSurface(int Id)
        {
            lock (thisLock )
            {
                Volsurface instance =
                    Engine.Instance.Session.CreateCriteria<Volsurface>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<Volsurface>();

                return instance;
            }
        }

        //Ported
        [ComVisible(false)]
        public IList<Volsurface> GetAllVolSurfacesSavedOnDate(DateTime date)
        {
            lock (thisLock )
            {
                string sql =
                    " select * from puma_mde_volsurface where trunc(timestamp)=trunc(:fordate) and volmodel_id=1 order by timestamp asc ";

                IList<Volsurface> instances = null;


                try
                {
                    instances = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(Volsurface))
                                .SetDateTime("fordate", date)
                                           .List<Volsurface>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting volsurfaces", e);
                }

                return instances;
            }
        }

        //Ported
        [ComVisible(false)]
        public IList<Volsurface> GetAllVolSurfacesSavedOnDate(DateTime date, Underlying und)
        {
            lock (thisLock )
            {
                string sql =
                    " select * from puma_mde_volsurface where trunc(timestamp)=trunc(:fordate) and underlying_id=:underlying and volmodel_id=1 order by timestamp asc ";

                IList<Volsurface> instances = null;


                try
                {
                    instances = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(Volsurface))
                                .SetDateTime("fordate", date)
                                    .SetInt32("underlying", und.Id)
                                           .List<Volsurface>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting volsurfaces", e);
                }

                return instances;
            }
        }

        //Ported
        public VolMonitors GetVolmonitorsForClassification(int classificationId)
        {
            lock (thisLock )
            {
                string sql = "select * from puma_mde_volmonitor where underlying_id in " +
                            "( select underlying_id from puma_mde_underlying_class where classification_id = " +
                            "( select id from puma_mde_classification where puma_mde_classification.id = :classid))";

                IList<VolMonitor> instances = null;

                try
                {
                    instances = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(VolMonitor))
                                .SetString("classid", classificationId.ToString())
                                           .List<VolMonitor>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting volmonitors", e);
                }

                if (instances == null)
                    return null;

                return new VolMonitors(instances);
            }
        }

        //Ported
        public VolMonitors GetVolmonitorsForClassification(Classification classification)
        {
            return GetVolmonitorsForClassification(classification.Id);
        }

        public Basis GetLastBasisForUnderlying(Underlying underlying)
        {
            return GetLastBasisForUnderlyingAt(underlying.Id, DateTime.Now);
        }

        public Basis GetLastBasisForUnderlyingAt(Underlying underlying, DateTime timestamp)
        {
            return GetLastBasisForUnderlyingAt(underlying.Id, timestamp);
        }

        public Basis GetLastBasisForUnderlyingAt(int underlyingId, DateTime timestamp)
        {
            lock (thisLock )
            {
                string sql =

                " select * from puma_mde_basis where id = ( " +
                " select " +
                "      max(b.id) " +
                " from  " +
                "     puma_mde_basis       b " +
                " where  " +
                "       b.underlying_id= :underlying_id " +
                " and   b.timestamp= " +
                " ( " +
                "    select  " +
                "         max(timestamp) " +
                "    from  " +
                "         puma_mde_basis" +
                "    where  " +
                "          underlying_id= :underlying_id " +
                "    and   timestamp < :timestamp " +
                " ) " +
                ")";

                Basis instance = null;

                try
                {
                    instance = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(Basis))
                                .SetInt32("underlying_id", underlyingId)
                                .SetDateTime("timestamp", timestamp)
                                        .UniqueResult<Basis>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting basis", e);
                }

                return instance;
            }
        }

        //Ported
        public Underlyings GetUnderlyingsWithBasis()
        {
            lock (thisLock )
            {
                string sql =

                " select * from puma_mde_underlying where id in ( " +
                " select distinct underlying_id from puma_mde_basis ) ";

                Underlyings instances = null;

                try
                {
                    instances = new Underlyings(Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(Underlying))
                                .List<Underlying>());
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting basis underlyings", e);
                }

                return instances;
            }
        }

        public CorporateActions GetCorporateActions()
        {
            lock (thisLock )
            {
                string sql =
                    "select ca.mdb_ca_id, ca.jour, sl.sicovam, ac.rfactor, ss.status_id " +
                    "from puma_corporate_actions ca " +
                    "join puma_ca_sicovam_list sl on (sl.mdb_ca_id = ca.MDB_CA_ID and ca.VERSION_ID = sl.version_id)" +
                    "join hvb_ca_sim_status ss on (ss.SICOVAM = sl.sicovam and ss.mdb_ca_id = ca.MDB_CA_ID and ca.VERSION_ID = ss.last_version) " +
                    "join HVB_CA_ACTIONS ac on (ac.mdb_ca_id = ca.mdb_ca_id and ac.sicovam = sl.sicovam) " +
                    "where ss.status_id in (6,2) and trunc(jour) = trunc(sysdate)";

                List<CorporateAction> cas = new List<CorporateAction>();
                CorporateActions instances = new CorporateActions(cas);
                List<object> queryResult = new List<object>();

                try
                {
                    Engine.Instance.
                        Session.CreateSQLQuery(sql).List(queryResult);
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting corporate actions", e);
                }

                if (queryResult.Count > 0) 
                {
                    
                    foreach (object[] res in queryResult)
                    {
                        CorporateAction ca = new CorporateAction();
                        ca.corprateActionId = System.Convert.ToInt32(res[0]);
                        ca.exDate = System.Convert.ToDateTime(res[1].ToString());
                        ca.underlyingSophisId = System.Convert.ToInt32(res[2]);
                        ca.rFactor = System.Convert.ToDouble(res[3]);
                        ca.underlying = GetUnderlyingFromSophisId(ca.underlyingSophisId);
                        instances.Add(ca);
                    }
                }

                return instances;
            }
        }

        public void DeleteAllAutoSpreadLinks(Int32 undId)
        {
            lock (thisLock )
            {
                NHibernate.ISQLQuery query = Engine.Instance.Session.CreateSQLQuery("delete from puma_mde_volsurface where underlying_id=:underlying_id and volmodel_id=(select id from puma_mde_volsurface_model where name=:model_name)");
                query.SetInt32("underlying_id", undId);
                query.SetString("model_name", string.Empty);

                query.ExecuteUpdate();
            }
        }

        public void DeleteAllAutoSpreadLinks(Underlying und)
        {
            DeleteAllAutoSpreadLinks(und.Id);
        }

        public bool HaveDividendsBeenChanged(Int32 sophisId)
        {
            lock (thisLock )
            {
                NHibernate.ISQLQuery query = Engine.Instance.Session.CreateSQLQuery("select count(*) from infos_histo where upper(nom_table)='DIVIDENDE' and sicovam=:sophis_id and trunc(date_validite)=date_to_num(trunc(:mydate))");

                query.SetInt32("sophis_id", sophisId);
                query.SetDateTime("mydate", Engine.Instance.Today);

                Decimal count = query.UniqueResult<Decimal>();
                return count > 0;
            }
        }

        public bool HaveReposBeenChanged(Int32 sophisId)
        {
            lock (thisLock )
            {
                NHibernate.ISQLQuery query = Engine.Instance.Session.CreateSQLQuery("select count(*) from infos_histo where upper(nom_table)='COUTPRETEMPRUNT' and sicovam=:sophis_id and trunc(date_validite)=date_to_num(trunc(:mydate))");

                query.SetInt32("sophis_id", sophisId);
                query.SetDateTime("mydate", Engine.Instance.Today);

                Decimal count = query.UniqueResult<Decimal>();
                return count > 0;
            }
        }

        public Calendar GetCalendar(int Id)
        {
            lock (thisLock )
            {
                Calendar Calendar =
                    Engine.Instance.Session.CreateCriteria<Calendar>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<Calendar>();

                return Calendar;
            }
        }

        public Calendars GetCalendars()
        {
            lock (thisLock )
            {
                IList<Calendar> instances =
                                   Engine.Instance.Session.CreateCriteria<Calendar>()
                                               .List<Calendar>();
                // enrich "last updated" field
                foreach (var cal in instances)
                {
                    if (!string.IsNullOrEmpty(cal.HolidayCode))
                    {
                        CalypsoCalendar calypsoCalendar = 
                            Engine.Instance.Session.Get<CalypsoCalendar>(cal.HolidayCode);
                        if (calypsoCalendar != null && calypsoCalendar.LastSophisUpdate != DateTime.MinValue)
                            cal.LastUpdate = calypsoCalendar.LastSophisUpdate;
                    }
                }
                return new Calendars(instances);
            }
        }

        public PreAfterMarketAdjustmentMonitors GetAlarmedPreAfterMarketAdjustmentMonitors(string workset)
        {
            lock (thisLock )
            {
                IList<PreAfterMarketAdjustmentMonitor> instances =
                    Engine.Instance.Session.CreateCriteria<PreAfterMarketAdjustmentMonitor>().Add(
                        Restrictions.Eq("Enabled", true)).Add(
                            Restrictions.Eq("Alarm", true)).Add(
                                Restrictions.Like("Workset", workset))
                                    .List<PreAfterMarketAdjustmentMonitor>();

                return new PreAfterMarketAdjustmentMonitors(instances);
            }
        }

        public IssuanceParameter GetIssuanceParameter(Classification c)
        {
            return GetIssuanceParameter(c.Id);
        }

        public IssuanceParameter GetIssuanceParameter(int classificationId)
        {
            lock (thisLock )
            {
                IssuanceParameter instance =
                    Engine.Instance.Session.CreateCriteria<IssuanceParameter>()
                    .Add(
                        Restrictions.Eq("ClassificationId", classificationId))
                            .UniqueResult<IssuanceParameter>();

                return instance;
            }
        }

        public ProductDescriptions GetProductDescriptions()
        {
            lock (thisLock )
            {
                IList<ProductDescription> instances =
                    Engine.Instance.Session.CreateCriteria<ProductDescription>()
                                .List<ProductDescription>();

                return new ProductDescriptions(instances);
            }
        }

        public IssuanceParameters GetIssuanceParameters(Classification c)
        {
            return GetIssuanceParameters(c.Id);
        }

        public IssuanceParameters GetIssuanceParameters(int classificationId)
        {
            lock (thisLock )
            {
                IList<IssuanceParameter> instance =
                    Engine.Instance.Session.CreateCriteria<IssuanceParameter>()
                    .Add(
                        Restrictions.Eq("ClassificationId", classificationId))
                           .List<IssuanceParameter>();

                return new IssuanceParameters(instance);
            }
        }

        public ProductDescription GetProductDescription(int descriptionId)
        {
            lock (thisLock )
            {
                ProductDescription instance = Engine.Instance.Session.CreateCriteria<ProductDescription>()
                    .Add(
                        Restrictions.Eq("Id", descriptionId))
                            .UniqueResult<ProductDescription>();

                return instance;
            }
        }

        public ProductDescription GetProductDescription(string name)
        {
            lock (thisLock )
            {
                ProductDescription instance = Engine.Instance.Session.CreateCriteria<ProductDescription>()
                    .Add(
                        Restrictions.Eq("PumaProductDescription", name))
                            .UniqueResult<ProductDescription>();

                return instance;
            }
        }

        //Ported
        public VolMonitors GetSophisRerefVolMonitors()
        {
            lock (thisLock )
            {
                IList<VolMonitor> instances;

                    instances =
                        Engine.Instance.Session.CreateCriteria<VolMonitor>().Add(
                                Restrictions.Eq("RerefEnabled", true))
                                    .List<VolMonitor>();

                return new VolMonitors(instances);
            }
        }

        public int GetNumberOfSophisUpdatesToday(Underlying und)
        {
            lock (thisLock )
            {
                return Convert.ToInt32(
                    Engine.Instance.Session.CreateSQLQuery(
                        "select count(*) from infos_histo h where sicovam=(select sophis_id from puma_mde_underlying where id=:underlying_id) and upper(nom_table)='VOLAT_INFOS' and trunc(date_validite)=date_to_num(trunc(sysdate))")
                            .SetInt32("underlying_id", und.Id)
                                .UniqueResult<Decimal>());
                    
            }
        }

        [ComVisible(false)]
        public IList<ETradingPlatform> GetAllETradingPlatforms()
        {
            lock (thisLock )
            {
                return
                    Engine.Instance.Session.CreateCriteria<ETradingPlatform>()
                                .List<ETradingPlatform>();
            }
        }

        private static readonly IList<string> _suffixesOfAmerica = new List<string> { "N", "OQ", "P", "TO" };
        private static readonly IList<string> _suffixesOfEurope = new List<string> { "AS", "BR", "CO", "DE", "HE", "I", "L", "MC", "MI", "OL", "PA", "S", "ST", "VI" };
        public static string GetMCAForUnderlyingByRIC(string ric)
        {
            var suffix = ric.Split(new char[] { '.' }).Last();

            if (_suffixesOfAmerica.Contains(suffix))
                return "America";

            if (_suffixesOfEurope.Contains(suffix))
                return "Europe";

            return string.Empty;
        }

        [ComVisible(false)]
        public IList<UnderlyingETradingPlatformApproval> GetAllPlatformApprovalsForUnderlying(int underlyingId)
        {
            lock (thisLock )
            {
                return
                    Engine.Instance.Session.CreateCriteria<UnderlyingETradingPlatformApproval>()
                        .Add(Restrictions.Eq("UnderlyingId", underlyingId))
                                .List<UnderlyingETradingPlatformApproval>();
            }
        }
        [ComVisible(false)]
        public void DeleteAllAprovalsForUnderlying(int underlyingId)
        {
            foreach (var approval in GetAllPlatformApprovalsForUnderlying(underlyingId))
            {
                Engine.Instance.Session.Delete(approval);
            }
        }

        [ComVisible(false)]
        public IList<string> GetYieldCurveNamesForFamilyCode(int familyCode)
        {
            lock (thisLock )
            {
                string sql =
                    " select libelle from Courbetaux where Codetypecourbetaux=:forfamilycode ";

                IList<string> instances = null;


                try
                {
                    instances = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                                .SetInt32("forfamilycode", familyCode)
                                           .List<string>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting yieldcurves from sophis", e);
                }

                return instances;
            }
        }

        [ComVisible(false)]
        public int GetYieldCurveFamilyCodeForName(string familyName, int currencyCode)
        {
            string sql =
                "select code from typecourbetaux where libelle =:forfamilyname and codedev =:forcurrencycode";

            decimal instance = 0;

            lock (thisLock )
            {
                try
                {
                    instance = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                                .SetString("forfamilyname", familyName)
                                .SetInt32("forcurrencycode", currencyCode)
                                .UniqueResult<Decimal>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting yield curve family code from sophis", e);
                }
            }

            return (Int32)instance;
        }

        [ComVisible(false)]
        public int GetCurrencyCodeForName(string currency)
        {
            string sql = "select str_to_devise('" + currency + "') from dual";

            decimal instance = 0;

            lock (thisLock)
            {
                try
                {
                    instance = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                                .UniqueResult<Decimal>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting currency code from sophis", e);
                }
            }

            return (Int32)instance;
        }

        [ComVisible(false)]
        public string GetYieldCurveFamilyNameForCode(int familyCode, int currencyCode)
        {
            string sql =
                "select libelle from typecourbetaux where code =:forfamilycode and codedev =:forcurrencycode";

            string instance = "";

            lock (thisLock )
            {
                try
                {
                    instance = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                                .SetInt32("forfamilycode", familyCode)
                                .SetInt32("forcurrencycode", currencyCode)
                                .UniqueResult<string>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting yield curve family name from sophis", e);
                }
            }

            return instance;
        }

        [ComVisible(false)]
        public string GetBasketFixingAlgorithm(Underlying und)
        {
            return GetBasketFixingAlgorithm(und.Id);
        }

        [ComVisible(false)]
        public string GetBasketFixingAlgorithm(int underlyingId)
        {
            string retval = null;

            string sql = "select BASKET_FIXING_ALGORITHM from puma_mde_underlying where id=:id";

            lock (thisLock )
            {
                try
                {
                    retval = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                                .SetInt32("id", underlyingId).UniqueResult<string>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting basket fixing algorithm", e);
                }
            }

            return retval;
        }

       

        public string GetClassificationPath(long clsId)
        {
            lock (thisLock )
            {
                try
                {
                    string sql =
                        " select path from puma_mde_classification ic, " +
                        " ( " +
                        " select child_id id, sys_connect_by_path(child_name, '/') path " +
                        " from ( " +
                        " select  " +
                        "      parent_id, child_id, p.name parent_name, c.name child_name " +
                        " from  " +
                        "      puma_mde_classification_hier h, " +
                        "      puma_mde_classification p, " +
                        "      puma_mde_classification c " +
                        " where " +
                        "      h.child_id=c.id " +
                        " and  h.parent_id=p.id " +
                        " ) " +
                        " connect by prior child_id=parent_id start with parent_id=(select id from puma_mde_classification where name='Everything') " +
                        " ) p " +
                        " where ic.id=p.id and ic.id=:classification_id"
                            ;

                    var result = Engine.Instance.Session.CreateSQLQuery(sql).AddScalar("path", NHibernate.NHibernateUtil.String).
                            SetInt64("classification_id", clsId).
                                List<string>();

                    string retval = String.Empty;
                    foreach (var one in result)
                    {
                        if (!String.IsNullOrEmpty(retval))
                            retval += ";";
                        retval += one;
                    }

                    return retval;
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting classification path", e);
                    return String.Empty;
                }
            }
        }

        [ComVisible(false)]
        public IList<LeveragedBarrierUnderlying> GetAllLeveragedBarrierUnderlyings(ISession session)
        {
            //lock (thisLock )
            {
                try
                {

                    var result = session.CreateCriteria<LeveragedBarrierUnderlying>().List<LeveragedBarrierUnderlying>();
                    return result;
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting leveraged barrier underlyings", e);
                    return null;
                }
            }
        }

        [ComVisible(false)]
        public IList<QuantoFee> GetAllQuantoFees(ISession session)
        {
            //lock (thisLock )
            {
                try
                {

                    var result = session.CreateCriteria<QuantoFee>().List<QuantoFee>();
                    return result;
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting quanto fees", e);
                    return null;
                }
            }
        }

        [ComVisible(false)]
        public List<string> GetEuwaxIsins(Underlying und, string productType)
        {
            IList<string> retval = null;

            string sql = "select isin from euwx_warrant where underlying=:reference";

            lock (thisLock )
            {
                try
                {
                    retval = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                                .SetString("reference", und.ISIN).List<string>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting Euwax ISINs", e);
                }
            }

            return retval.ToList<string>();
        }

        public YieldCurveMonitorChild GetYieldCurveMonitorChild(int Id)
        {
            lock (thisLock )
            {
                YieldCurveMonitorChild instance =
                    Engine.Instance.Session.CreateCriteria<YieldCurveMonitorChild>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<YieldCurveMonitorChild>();

                return instance;
            }
        }

        public PddaMonitor GetPddaMonitor(string workset)
        {
           lock (thisLock )
           {
              PddaMonitor instance =
                    Engine.Instance.Session.CreateCriteria<PddaMonitor>().Add(
                        Restrictions.Eq("Workset", workset))
                            .UniqueResult<PddaMonitor>();

              return instance;
           }
        }

        public PddaMonitors GetPddaMonitors(string workset)
        {
            lock (thisLock )
            {
                IList<PddaMonitor> instances =
                    Engine.Instance.Session.CreateCriteria<PddaMonitor>().Add(
                        Restrictions.Eq("Enabled", true)).Add(
                            Restrictions.Like("Workset", workset))
                                .List<PddaMonitor>();

                return new PddaMonitors(instances);
            }
        }

        public PddaMonitor GetPDDAMonitor(int Id)
        {
            lock (thisLock )
            {
                PddaMonitor instance =
                    Engine.Instance.Session.CreateCriteria<PddaMonitor>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<PddaMonitor>();

                return instance;
            }
        }

        internal AiAlgorithmLink GetAiAlgorithmForPddaMonitor(int Id)
        {
            lock (thisLock )
            {
                AiAlgorithmLink instance =
                    Engine.Instance.Session.CreateCriteria<AiAlgorithmLink>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<AiAlgorithmLink>();

                return instance;
            }
        }

        internal AiAlgorithm GetAiAlgorithm(int Id)
        {
            lock (thisLock )
            {
                AiAlgorithm instance =
                    Engine.Instance.Session.CreateCriteria<AiAlgorithm>().Add(
                        Restrictions.Eq("Id", Id))
                            .UniqueResult<AiAlgorithm>();

                return instance;
            }
        }

        public AiAlgorithms GetAiAlgorithms()
        {
            lock (thisLock )
            {
                IList<AiAlgorithm> instances =
                    Engine.Instance.Session.CreateCriteria<AiAlgorithm>()
                                .List<AiAlgorithm>();

                return new AiAlgorithms(instances);
            }
        }

        public AiAlgorithmLink GetAiAlgorithmLinkForPddaMonitor(PddaMonitor pddaMonitor)
        {
            return GetAiAlgorithmLinkForPddaMonitor(pddaMonitor.Id);
        }

        public AiAlgorithmLink GetAiAlgorithmLinkForPddaMonitor(int pddaMonitorId)
        {
            lock (thisLock )
            {
                AiAlgorithmLink instance =
                    Engine.Instance.Session.CreateCriteria<AiAlgorithmLink>().Add(
                        Restrictions.Eq("PddaMonitorId", pddaMonitorId))
                            .UniqueResult<AiAlgorithmLink>();

                return instance;
            }
        }

        public AiAlgorithm GetAiAlgorithm(string name)
        {
            lock (thisLock )
            {
                AiAlgorithm instance =
                    Engine.Instance.Session.CreateCriteria<AiAlgorithm>().Add(
                        Restrictions.Eq("Name", name))
                            .UniqueResult<AiAlgorithm>();

                return instance;
            }
        }

        public AiGenerations GetLastAiGenerations(AiAlgorithm algorithm)
        {
            return GetLastAiGenerations(algorithm.Id);
        }

        public AiGeneration GetLastAiGeneration(int algoId)
        {
            lock (thisLock)
            {
                string sql = "select * from puma_mde_ai_generation " +
                             "where id in (" +
                             "    select id from puma_mde_ai_generation " +
                             "    where algo_id = :algoId and validity_date = (" +
                             "        select max(validity_date) from puma_mde_ai_generation " +
                             "        where algo_id = :algoId and trunc(validity_date)<=trunc(sysdate)))";

                AiGeneration instance = null;

                try
                {
                    instance = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(AiGeneration))
                                .SetInt32("algoId", algoId)
                                    .UniqueResult<AiGeneration>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("Error while getting last Generation", e);
                }

                if (instance == null)
                    return null;

                return instance;
            }
        }

        public AiGenerations GetLastAiGenerations(int algoId)
        {
            lock (thisLock)
            {
                string sql =
                        " select * from puma_mde_ai_generation where id in " +
                        " (select id from puma_mde_ai_generation where algo_id = :algoId and validity_date = (select max(validity_date) from puma_mde_ai_generation where algo_id = :algoId)) ";

                IList<AiGeneration> instances = null;

                try
                {
                    instances = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddEntity(typeof(AiGeneration))
                                .SetInt32("algoId", algoId)
                                    .List<AiGeneration>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting Generations", e);
                }

                if (instances == null)
                    return null;

                return new AiGenerations(instances);
            }
        }

        [ComVisible(false)]
        public PddaMonitors GetUnprocessedPddaMonitors(ISession session)
        {
            //lock (thisLock )
            {
                IList<PddaMonitor> instances = null;

                try
                {
                    instances = 
                    session.CreateCriteria<PddaMonitor>().
                            Add(Restrictions.Eq("Acknowledged", false)).
                               List<PddaMonitor>();
                }
                catch(Exception e)
                {
                    Engine.Instance.ErrorException("error while getting unproccesed PDDA monitors", e);
                }
                return new PddaMonitors(instances);
            }
        }

        public AiSignal GetLastSignal(PddaMonitor mon)
        {
            string sql =
                    " select * from puma_mde_ai_signals where id = " +
                    " (select max(id) from puma_mde_ai_signals where Monitor_id = :monId) ";

            lock (thisLock )
            {
                AiSignal instance = null;
                try
                {
                    instance = Engine.Instance.
                    Session.CreateSQLQuery(sql)
                        .AddEntity(typeof(AiSignal))
                            .SetInt32("monId", mon.Id)
                                .UniqueResult<AiSignal>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting last PDDA signal", e);
                }

                return instance;
            }
        }

        [ComVisible(false)]
        public IList<AiWeight> GetPDDAWeights(int genID)
        {
            string sql = "select * from puma_mde_ai_weights where gen_id=:genID";

            lock (thisLock )
            {
                IList<AiWeight> instances = null;
                try
                {
                    instances = Engine.Instance.Session.CreateSQLQuery(sql)
                                .AddEntity(typeof(AiWeight))
                                    .SetInt32("genID", genID)
                                        .List<AiWeight>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting PDDA weights", e);
                }
                return instances;
            }
        }

        public Data.UniConfig GetConfig(string category, string key)
        {
           string sql = "select * from uni_config where lower(category)=lower(:cat) " +
                         "and lower(key) = lower(:k)";

           lock (thisLock )
           {
              Data.UniConfig instances = null;
              try
              {
                 instances = Engine.Instance.Session.CreateSQLQuery(sql)
                     .AddEntity(typeof(Data.UniConfig))
                     .SetString("cat", category)
                     .SetString("k", key)
                     .UniqueResult<Data.UniConfig>();
              }
              catch (Exception e)
              {
                 Engine.Instance.ErrorException("Error while checking configuration", e);
              }
              return instances;
           }
        }

        // returns valid user or null if user is not found
        public AiSubscriber CheckSignalSubscriber(string userName)
        {
            string sql = "select * from PUMA_MDE_AI_SUBSCRIBERS where username=lower(:userName) " +
                         "and trunc(sysdate) between trunc(valid_from) and trunc(valid_to)";

            lock (thisLock )
            {
                AiSubscriber instances = null;
                try
                {
                    instances = Engine.Instance.Session.CreateSQLQuery(sql)
                        .AddEntity(typeof (AiSubscriber))
                        .SetString("userName", userName)
                        .UniqueResult<AiSubscriber>();                      
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("Error while checking subscribers", e);
                }
                return instances;
            }
        }



        [ComVisible(false)]
        public IList<string> GetAIUnderlyingsList()
        {
            string sql = "select unique mul.reference from puma_mde_underlying mul join puma_mde_ai_vola_underlying vul on vul.underlying_id = mul.id order by mul.reference";

            lock (thisLock)
            {
                IList<string> underlyings = new List<string>();
                try
                {
                    underlyings = Engine.Instance.Session.CreateSQLQuery(sql).List<string>();
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("error while getting available VolAI underlyings", e);
                }
                return underlyings;
            }
        }



        public string GetResponsibleDeskForUnderlying(int underlyingId)
        {
            string sql = @"select distinct p.path path
                            from (
                                   select path, ic.underlying_id
                                   from puma_mde_underlying_class ic,
                                        (
                                          select child_id id, sys_connect_by_path(child_name, '//') path
                                          from (
                                                 select parent_id,
                                                        child_id,
                                                        p.name parent_name,
                                                        c.name child_name
                                                 from puma_mde_classification_hier h,
                                                      puma_mde_classification p,
                                                      puma_mde_classification C
                                                 where h.child_id = C.ID
                                                   and h.parent_id = p.id
                                               )
                                          connect by prior child_id = parent_id
                                          start with parent_id in (select id from puma_mde_classification where name in ('Everything'))
                                        ) p
                                   where ic.classification_id = p.ID
                                 ) p,
                                 titres t,
                                 puma_mde_underlying u
                            where u.ID = p.underlying_id
                              and u.id = :undlId
                              and u.sophis_id = t.sicovam
                              and ( p.path like '//%Stock//%' 
                                    or p.path like '//%Stocks//%' 
                                    or p.path like '//%Index//%' 
                                    or p.path like '//%Indices%//%')
                            ";
            lock (thisLock)
            {
                try
                {
                    var res = Engine.Instance.
                        Session.CreateSQLQuery(sql)
                            .AddScalar("path", NHibernate.NHibernateUtil.String)
                                .SetInt32("undlId", underlyingId)
                                    .List<string>();

                    foreach (var path in res)
                    {
                        var pathElems = path.Split(new string[] { "//" }, StringSplitOptions.RemoveEmptyEntries)
                            .Where(p => p.Contains("Stock") || p.Contains("Index") || p.Contains("Indices"));
                        if (pathElems.Any())
                        {
                            var undlClass = pathElems.First();

                            if (undlClass.Contains("Single Stock"))
                            {
                                return "Single Stock";
                            }

                            if (undlClass.Contains("Multi"))
                            {
                                return "Correlation";
                            }

                            if (undlClass.Contains("Index") && undlClass.Contains("Indices"))
                            {
                                return "Single Index";
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Engine.Instance.ErrorException("Error while getting responsible desk", e);
                }
            }

            return String.Empty;
        }

        [ComVisible(false)]
        public IList<UnderlyingLastUpdate> GetUnderlyingLastUpdates(int sophisId, int underlyingId, int cnt)
        {
            var sql = @"select oo.updatedt_orc as ""ORCDt"", os.updatedt_sophis as ""SophisDt""
                        from
                          (
                            select rownum as srn, s.*
                            from
                              (
                              select num_to_date(date_validite) as updatedt_sophis
                              from infos_histo
                              where nom_table = 'VOLAT_INFOS'
                              and sicovam = :sicovam
                              order by date_validite desc
                              ) s
                            where rownum <= :cnt
                          ) os,
                          (
                            select rownum as orn, o.*
                            from
                              (
                              select timestamp as updatedt_orc
                              from puma_mde_volsurface
                              where underlying_id = :underlyingId
                               and volmodel_id = 1
                              order by timestamp desc
                              ) o
                            where rownum <= :cnt
                          ) oo
                        where os.srn = oo.orn";
            var res = new List<UnderlyingLastUpdate>();

            lock (thisLock)
            {
                try
                {
                    res = Engine.Instance.Session
                        .CreateSQLQuery(sql)
                        .SetResultTransformer(Transformers.AliasToBean<UnderlyingLastUpdate>())
                        .SetInt32("sicovam", sophisId)
                        .SetInt32("underlyingId", underlyingId)
                        .SetInt32("cnt", cnt)
                        .List<UnderlyingLastUpdate>().ToList();
                }
                catch (Exception ex)
                {
                    Engine.Instance.InfoException("Underlying's Last Updates weren't retrieved", ex);
                }
            }

            return res;
        }

        [ComVisible(false)]
        public IList<BucketedVegaRecord> GetBucketedVega(int sophisId)
        {
            var sql = @"select * from 
                        (
                          select id, portfolio, sophis_id, bucket, bucket_date, vega from nxs_bucketed_vega where sophis_id=:sophis_id
                          union
                          select -rownum-:sophis_id, portfolio, sophis_id, bucket, bucket_date, vega from 
                          (
                            select portfolio, sophis_id, 'Total' bucket, null bucket_date, sum(vega) vega from nxs_bucketed_vega where sophis_id=:sophis_id group by sophis_id, portfolio
                          )
                        )
                        order by portfolio, bucket_date nulls last";

            var res = new List<BucketedVegaRecord>();

            lock (thisLock)
            {
                try
                {
                    res = Engine.Instance.Session
                        .CreateSQLQuery(sql)
                        .AddEntity(typeof(BucketedVegaRecord))
                        .SetInt32("sophis_id", sophisId)
                        .List<BucketedVegaRecord>()
                        .ToList();
                }
                catch (Exception ex)
                {
#if SOPHIS_7
                    Engine.Instance.Log.Info(ex,"Underlying's bucketed vega wasn't retrieved" );
#else
                    Engine.Instance.InfoException("Underlying's bucketed vega wasn't retrieved", ex);
#endif
                }
            }

            return res;
        }

        [ComVisible(false)]
        public IList<BucketedEpsilonRecord> GetBucketedEpsilon(int sophisId)
        {
            var sql = @"select * from 
                        (
                          select id, portfolio, sophis_id, bucket, bucket_date, epsilon from nxs_bucketed_epsilon where sophis_id=:sophis_id
                          union
                          select -rownum-:sophis_id, portfolio, sophis_id, bucket, bucket_date, epsilon from 
                          (
                            select portfolio, sophis_id, 'Total' bucket, null bucket_date, sum(epsilon) epsilon from nxs_bucketed_epsilon where sophis_id=:sophis_id group by sophis_id, portfolio
                          )
                        )
                        order by portfolio, bucket_date nulls last";

            var res = new List<BucketedEpsilonRecord>();

            lock (thisLock)
            {
                try
                {
                    res = Engine.Instance.Session
                        .CreateSQLQuery(sql)
                        .AddEntity(typeof(BucketedEpsilonRecord))
                        .SetInt32("sophis_id", sophisId)
                        .List<BucketedEpsilonRecord>()
                        .ToList();
                }
                catch (Exception ex)
                {
#if SOPHIS_7
                    Engine.Instance.Log.Info(ex,"Underlying's bucketed epsilon wasn't retrieved");
#else
                    Engine.Instance.InfoException("Underlying's bucketed epsilon wasn't retrieved", ex);
#endif
                }
            }

            return res;
        }

        [ComVisible(false)]
        public IEnumerable<(int Sicovam1, int Sicovam2, string MaturityLabel, DateTime MaturityDate, double CorrelationInPercent)> GetCorrelations(int sicovam)
        {
            Engine.Instance.Log.Info($"Querying correlations for {sicovam}...");

            lock (thisLock)
            {
                try
                {
                    var rawResults = Engine.Instance.Session.CreateSQLQuery(
                              @"
                                SELECT t1.sicovam sicovam1, t2.sicovam sicovam2, g.x maturity, (case when t1.type = 'E' or t2.type = 'E' then -1 else 1 end) * g.y correlation, g.type FROM GR_INFOSCOURBE ic 
                                join gr_points g ON ic.ident = g.courbe
                                join correlation_rco c on c.graphe = ic.graphe
                                join titres t1 on c.ident1 = t1.sicovam and t1.type in ('A', 'I', 'D', 'E')
                                join titres t2 on c.ident2 = t2.sicovam and t2.type in ('A', 'I', 'D', 'E')
                                where t1.sicovam = :sicovam
                            union all
                                SELECT t1.sicovam sicovam1, t2.sicovam sicovam2, g.x maturity, (case when t1.type = 'E' or t2.type = 'E' then -1 else 1 end) * g.y correlation, g.type FROM GR_INFOSCOURBE ic 
                                join gr_points g ON ic.ident = g.courbe
                                join correlation_rco c on c.graphe = ic.graphe
                                join titres t1 on c.ident2 = t1.sicovam and t1.type in ('A', 'I', 'D', 'E')
                                join titres t2 on c.ident1 = t2.sicovam and t2.type in ('A', 'I', 'D', 'E')
                                where t1.sicovam = :sicovam
                        ")
                        .SetParameter("sicovam", sicovam)
                        .List<object[]>();

                    return null;
                } 
                catch (Exception ex)
                {
                    Engine.Instance.Log.Error(ex, $"Cannot retrieve correlations for {sicovam}");
                    throw;
                }
            }
        }

        [ComVisible(false)]
        public (int DivType, int DivFactor, int RepoType, int RepoFactor) GetMCDivRepoFactors(int sicovam)
        {
            Engine.Instance.Log.Info($"Querying MC parameters for Sicovam {sicovam}...");

            lock (thisLock)
            {
                try
                {
                    var resultList = Engine.Instance.Session.CreateSQLQuery(
                        @"
                            SELECT nvl(t.hvb_multicurr_div, 0), nvl(t.hvb_multicurr_div_factor, 0), nvl(t.hvb_multicurr_repo, 0), nvl(t.hvb_multicurr_repo_factor, 0) from titres t
                            where t.sicovam = :sicovam and t.modele in ('Extended MultiCurrency', 'Dynamic MultiCurrency')
                        ")
                        .SetParameter("sicovam", sicovam)
                        .List<object[]>();

                    if (!resultList.Any())
                    {
                        return (0, 0, 0, 0);
                    }

                    var r = resultList.Single();

                    return (
                        DivType: Convert.ToInt32(r[0]),
                        DivFactor: Convert.ToInt32(r[1]),
                        RepoType: Convert.ToInt32(r[2]),
                        RepoFactor: Convert.ToInt32(r[3])
                    );
                }
                catch (Exception ex)
                {
                    Engine.Instance.Log.Error(ex, $"Cannot retrieve MC parameters for {sicovam}");
                    throw;
                }
            }
        }

        public IEnumerable<(string MXLabel, int Sicovam, string Reference, string Type)> GetMXToSophisEquityMapping()
        {
            Engine.Instance.Log.Info($"Querying MX to Sophis Mapping...");

            lock (thisLock)
            {
                try
                {
                    var resultList = Engine.Instance.Session.CreateSQLQuery(
                        @"
                            SELECT mxdisplaylabel, t.sicovam, t.reference, u.type from EQMX_MD_UNIVERSE u
                            join titres t on t.reference = u.reference
                        ")
                        .List<object[]>();

                    return resultList
                        .Select(r =>
                            (
                                MXLabel: Convert.ToString(r[0]),
                                Sicovam: Convert.ToInt32(r[1]),
                                Reference: Convert.ToString(r[2]),
                                Type: Convert.ToString(r[3])
                            )
                        );
                }
                catch (Exception ex)
                {
                    Engine.Instance.Log.Error(ex, $"Cannot retrieve MX to Sophis Equity Mapping");
                    throw;
                }
            }
        }
    }
}
