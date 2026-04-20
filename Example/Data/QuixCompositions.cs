using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Puma.MDE.Data
{
    [ComVisible(false)]
    public class QuixCompositions : List<QuixComposition>
    {
        public bool IsDirty(out string errorMessage)
        {
            errorMessage = null;

            var tosave = false;
            foreach (var quixComposition in this)
            {
                var currentQuixComposition = quixComposition;

                // first check parent composition is dirty
                tosave |= currentQuixComposition.IsDirty();

                // Do we have components
                if (currentQuixComposition.HasComponents)
                {
                    var isAllMarkedToBeDeleted = currentQuixComposition.QuixComponents.All(c => c.IsSetToDelete);
                    if (isAllMarkedToBeDeleted)
                    {
                        errorMessage = "Quix composition must have atleast one component.";
                        return false;

                    }

                    // Check if there is atleast one component is either to be deleted or changed
                    foreach (var component in currentQuixComposition.QuixComponents)
                    {
                        var curComponent = component;

                        // if marked to be deleted from Db means is not dirty but not needed anymore  
                        // we flag that there is something to save
                        tosave |= curComponent.IsSetToDelete;

                        // If it has changed we want to make sure to flag there is something to save
                        tosave |= curComponent.IsDirty();

                        if (tosave)
                            break;
                    }
                }
                else
                {
                    // if is not markted to delete and we want to save it , we have to make sure that it has atleast one
                    if (currentQuixComposition.IsSetToDelete == false)
                    {
                        errorMessage = "Quix composition must have atleast one component.";
                        return false;

                    }
                }

                // Check the final for this composition
                if (tosave)
                {
                    break;
                }
            }

            return tosave;
        }

        public QuixComposition LastAvailableQuixCompositionFromToday()
        {
            if (Count > 0)
            {
                // Represent tomorrow
                var todayAtMidnight = DateTime.Now.Date.AddDays(1);

                // Since we may possibly have compositions in future we have to get valid from that is smaller than future date
                var listOfCompositionFromToday = this.Where(c => c.ValidFrom.Date < todayAtMidnight)
                                                                 .OrderByDescending(c => c.ValidFrom);

                return listOfCompositionFromToday.FirstOrDefault();
            }

            return null;
        }

        public QuixComposition LastAvailableQuixCompositionBefore(DateTime date)
        {
            return this.OrderByDescending(c => c.ValidFrom).FirstOrDefault(c => c.ValidFrom < date);
        }

        public QuixComposition LastAvailableQuixCompositionBefore(QuixComposition composition)
        {
            return this.OrderByDescending(c => c.ValidFrom).ToList()
                       .Find(thisCompo =>
                           thisCompo.ValidFrom.Date < composition.ValidFrom.Date &&
                           thisCompo.ReweightStart.Date < composition.ReweightStart.Date
                        );
        }

        public QuixComposition LastAvailableQuixCompositionBeforeToday(QuixComposition composition)
        {
            var today = DateTime.Now.Date;
            var reweightStart = composition.ReweightStart.Date;
            var available = this.OrderByDescending(c => c.ValidFrom)
                                .Where(c => c.ValidFrom.Date < today &&            // Repreasent Yesterday check (for anyIntermediate)
                                            c.ReweightStart.Date == reweightStart) // With same reweight start
                                .OrderByDescending(c => c.ValidFrom);

            return available.FirstOrDefault();
        }

        // max 1 composition per validity date is allowed generally
        // max 2 compositions per validity date are allowed, provided:
        //  * distinct Reweight Start dates
        //  * model name containing substring "Manual Fee"
        public bool AddQuixComposition(QuixComposition quixComposition, Underlying indexUnderlying)
        {
            if (Contains(quixComposition))
                return false;

            if (quixComposition.Underlying.QuixReweightName.StartsWith("Manual-Fee") || quixComposition.Underlying.QuixReweightName.StartsWith("Standard-Fee"))   // TRAC: #34799
            {
                var anyExistWithSameValidFromAndReweightFrom = this.Any(
                    c => (c.ValidFrom.Date.Equals(quixComposition.ValidFrom.Date)) &&
                         (c.ReweightStart.Date.Equals(quixComposition.ReweightStart.Date)));

                if (anyExistWithSameValidFromAndReweightFrom)
                    return false;
            }
            else
            {
                var anyExistWithSameValidFrom = this.Any(
                    c => c.ValidFrom.Date.Equals(quixComposition.ValidFrom.Date));

                if (anyExistWithSameValidFrom)
                    return false;
            }

            quixComposition.Underlying = indexUnderlying;
            Add(quixComposition);

            indexUnderlying.SetToDirty(() => quixComposition);

            return true;
        }

        public bool UpdateQuixComposition(QuixComposition quixComposition, Underlying indexUnderlying)
        {

            // If it exist remove it and add new one
            var anyExistWithSameValidFrom = this.SingleOrDefault(c => c.ValidFrom.Date.Equals(quixComposition.ValidFrom.Date));
            if (anyExistWithSameValidFrom != null)
            {
                Remove(anyExistWithSameValidFrom);
            }

            quixComposition.Underlying = indexUnderlying;
            Add(quixComposition);

            indexUnderlying.SetToDirty(() => quixComposition);

            return true;
        }

        public void RemoveQuixComposition(QuixComposition quixComposition)
        {
            if (quixComposition.IsPersisted)
            {
                quixComposition.IsSetToDelete = true;
                quixComposition.QuixComponents.ToList().ForEach(component => component.IsSetToDelete = true);
            }
            else
            {
                if (Contains(quixComposition))
                {
                    Remove(quixComposition);
                }
            }
        }

        public void RemoveAnySetToDelete()
        {
            RemoveAll(composition => composition.IsSetToDelete);
        }

        public void ResetToNotDirty()
        {
            foreach (var quixComposition in this)
            {
                var currQuixComposition = quixComposition;

                currQuixComposition.ResetToNonDirty();

                foreach (var comp in currQuixComposition.QuixComponents)
                {
                    var currComp = comp;
                    currComp.ResetToNonDirty();
                }
            }
        }

        
    }
}
