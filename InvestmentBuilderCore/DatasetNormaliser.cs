using System.Collections.Generic;
using System.Linq;

namespace InvestmentBuilderCore
{ 
    public static class DatasetNormaliser
    {
        /// <summary>
        /// this helper method takes a dataset and returns it with no more
        /// than the number of elements specified by window. The returned
        /// data is evenly distributed as the data range 
        /// this is used for reports where only a certain number of datapoints
        /// can be displayed. will always include the first data point and
        /// should ideally include the last one
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="window"></param>
        /// <returns></returns>
        public static IList<T> NormaliseDataset<T>( IList<T> data, int window) where T : class
        {
            var workingList = data.ToList();
            if(workingList.Count > window)
            {
                var remain = workingList.Count % window;
                var count = 1;
                //-1,1,3,5
                while(remain > 0)
                {
                    //need to start removing some data points. do this from the
                    //start so the distribution result is more weighted towards
                    //later data points
                    workingList.RemoveAt(count++);
                    remain = workingList.Count % window;
                }

                //now have a dataset size that is a multiplication of the window
                var increment = workingList.Count / window;
                var result = new List<T>();
                count = 0;
                while(count < workingList.Count)
                {
                    result.Add(workingList[count]);
                    count += increment;
                }
                //now a small fudge. ensure that the last entry is the same
                //as the last entry in the dataset
                if(result.Last() != data.Last())
                {
                    result.Remove(result.Last());
                    result.Add(data.Last());
                }
                return result;
            }
            return workingList;
        }
    }
}
