using System.Collections.Generic;
using System.Threading.Tasks;
using RTLolaMo3Vis.Models;

namespace RTLolaMo3Vis.Services
{
    public interface IDataStore<T, K>
    {
        void AddItem(T item);

        /*
         * deletes item by internal StringId
         */
        /*
         * deletes item by it's id
         */
        T GetItemString(string id);

        T GetItem(byte id);

        IEnumerable<K> GetAllItems();

        void DeleteAllItems();
    }
}
