//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using Itinero.Data.Attributes;
//using OsmSharp.Tags;
//
//namespace Itinero.IO.Osm.Collections
//{
//    /// <summary>
//    /// A tag collection as an attribute collection.
//    /// </summary>
//    public class TagAttributeCollection : IEnumerable<(string key, string value)>
//    {
//        private readonly TagsCollectionBase _tagCollection;
//
//        /// <summary>
//        /// Creates a tag attribute collection.
//        /// </summary>
//        public TagAttributeCollection(TagsCollectionBase tagsCollection)
//        {
//            _tagCollection = tagsCollection;
//        }
//        /// <summary>
//        /// Gets the count.
//        /// </summary>
//        public int Count => _tagCollection.Count;
//
//        /// <summary>
//        /// Gets the readonly flag.
//        /// </summary>
//        public bool IsReadonly => false;
//
//        /// <summary>
//        /// Adds or replaces an attribute.
//        /// </summary>
//        public void AddOrReplace(string key, string value)
//        {
//            _tagCollection.AddOrReplace(key, value);
//        }
//
//        /// <summary>
//        /// Clears all attributes.
//        /// </summary>
//        public void Clear()
//        {
//            _tagCollection.Clear();
//        }
//
//        /// <summary>
//        /// Gets the enumerator.
//        /// </summary>
//        /// <returns></returns>
//        public IEnumerator<Attribute> GetEnumerator()
//        {
//            return _tagCollection.Select(x => new Attribute(x.Key, x.Value)).GetEnumerator();
//        }
//
//        /// <summary>
//        /// Removes the attribute with the given key.
//        /// </summary>
//        public bool RemoveKey(string key)
//        {
//            return _tagCollection.RemoveKey(key);
//        }
//
//        /// <summary>
//        /// Tries to get the value for the given key.
//        /// </summary>
//        public bool TryGetValue(string key, out string value)
//        {
//            return _tagCollection.TryGetValue(key, out value);
//        }
//
//        /// <summary>
//        /// Gets the enumerator.
//        /// </summary>
//        /// <returns></returns>
//        IEnumerator IEnumerable.GetEnumerator()
//        {
//            return _tagCollection.Select(x => new Attribute(x.Key, x.Value)).GetEnumerator();
//        }
//    }
//}