using System;
using System.Linq;
using System.Linq.Expressions;
using Puma.MDE.Common.Utilities;

namespace Puma.MDE.Common.ViewModel
{
    public class ViewModelBase : Entity
    {

        /// <summary>
        /// Observe as many properties from the CURRENT instance class (must be in the class, perhaps constructor)
        /// and when they ALL are set, the delgate is called to notify property values have been changed
        /// ObserveProperties
        /// (
        ///    () => {   },  // Action ready delegate to be called. you can put any thing to be executed with in Curly braces
        ///    () => Property1,
        ///    () => Property2,
        ///    () => ...
        /// );
        /// An real example would be
        /// 
        /// TurnOffWaitCursor = true
        /// ObserveProperties
        /// (
        ///    () => { TurnOffWaitCursor = false  },
        ///    () => ProblemListCutOffs,
        ///    () => ProblemListFilterAndSettings,
        ///    () => UnderlyingChanges
        /// );
        /// </summary>
        /// <param name="ready">Callback that is called</param>
        /// <param name="propertyExtensions">()=> property1, ()=> property2 and so on </param>
        protected void ObserveProperties(Action ready, params Expression<Func<object>>[] propertyExtensions)
        {
            if (propertyExtensions == null)
                return;

            IObservable<object> observable = null;
            var list = propertyExtensions.ToList();

            // If there is only one observe on that property
            if (list.Count >= 1)
            {
                observable = ReactiveEx.ObservableProperty(propertyExtensions[0]);
            }

            // Combine with others if there are more than one property to observe, start from index 1 
            for (var index = 1; index < list.Count; index++)
            {
                var otherObservable = ReactiveEx.ObservableProperty(propertyExtensions[index]);
                //observable = observable.CombineLatest(otherObservable, (a, b) => b);
            }
        }

        protected void ObserveProperties(Action ready, object propertyValue, params Expression<Func<object>>[] propertyExtensions)
        {
            if (propertyExtensions == null)
                return;

            IObservable<object> observable = null;
            var list = propertyExtensions.ToList();

            // If there is only one observe on that property
            if (list.Count >= 1)
            {
                observable = ReactiveEx.ObservableProperty(propertyValue, propertyExtensions[0]);
            }

            // Combine with others if there are more than one property to observe, start from index 1 
            for (var index = 1; index < list.Count; index++)
            {
                var otherObservable = ReactiveEx.ObservableProperty(propertyValue, propertyExtensions[index]);
                //observable = observable.CombineLatest(otherObservable, (a, b) => b);
            }
        }

        /// <summary>
        /// Observe as many properties from the ANY instance class (must be in the class that observe other instances, perhaps constructor) 
        /// and when they ALL are set, the delgate is called to notify property values have been changed
        /// Ex
        ///  ObserveProperties(CombineTradingAndRiskChartData, new object[,] { 
        ///        { _volatilitySurfaceDataTrading.VolatilitySliceChartData, "LoadCompleted" },
        ///        { _volatilitySurfaceDataRisk.VolatilitySliceChartData,    "LoadCompleted" }
        ///    });
        /// </summary>
        /// <param name="ready">Call back</param>
        /// <param name="propertyExtensions">Pairs of instances and string property that we are observing { anInstance, "PropertyName" }</param>
        protected void ObserveProperties(Action ready, object[,] propertyExtensions)
        {
            IObservable<object> observable = null;

            var length = propertyExtensions.GetLength(0);
            // If there is only one observe on that property
            if (length >= 1)
            {
                var fromObject = propertyExtensions[0, 0];
                var propertyName = (string)propertyExtensions[0, 1];
                observable = ReactiveEx.ObservablePropertyFrom(fromObject, propertyName);
            }

            // Combine with others if there are more than one property to observe, start from index 1 
            for (var index = 1; index < length; index++)
            {
                var fromObject = propertyExtensions[index, 0];
                var propertyName = (string)propertyExtensions[index, 1];

                var otherObservable = ReactiveEx.ObservablePropertyFrom(fromObject, propertyName);
                //observable = observable.CombineLatest(ReactiveEx.ObservablePropertyFrom(fromObject, propertyName), (a, b) => b);
            }
        }

        protected void ObserveProperties(Action ready, object propertyValue, object[,] propertyExtensions)
        {
            IObservable<object> observable = null;

            var length = propertyExtensions.GetLength(0);
            // If there is only one observe on that property
            if (length >= 1)
            {
                var fromObject = propertyExtensions[0, 0];
                var propertyName = (string)propertyExtensions[0, 1];
                observable = ReactiveEx.ObservablePropertyFrom(fromObject, propertyValue, propertyName);

            }

            // Combine with others if there are more than one property to observe, start from index 1 
            for (var index = 1; index < length; index++)
            {
                var fromObject = propertyExtensions[index, 0];
                var propertyName = (string)propertyExtensions[index, 1];

                var otherObservable = ReactiveEx.ObservablePropertyFrom(fromObject, propertyValue, propertyName);
                //observable = observable.CombineLatest(otherObservable, (a, b) => b);
            }
        }
    }
}
