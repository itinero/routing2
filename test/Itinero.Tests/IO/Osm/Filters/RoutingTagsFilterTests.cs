using Itinero.IO.Osm.Filters;
using OsmSharp;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.IO.Osm.Filters
{
    public class RoutingTagsFilterTests
    {
        [Fact] 
        public void RoutingTagsFilter_Filter_Node_Null_ShouldBeExcluded()
        {
            var filter = RoutingTagsFilter.Default?.Filter?.Invoke(new Node() {
                Tags = null
            });
            
            Assert.Null(filter);
        }
        
        [Fact] 
        public void RoutingTagsFilter_Filter_Way_Null_ShouldBeExcluded()
        {
            var filter = RoutingTagsFilter.Default?.Filter?.Invoke(new Way() {
                Tags = null
            });
            
            Assert.Null(filter);
        }
        
        [Fact] 
        public void RoutingTagsFilter_Filter_Relation_Null_ShouldBeExcluded()
        {
            var filter = RoutingTagsFilter.Default?.Filter?.Invoke(new Relation() {
                Tags = null
            });
            
            Assert.Null(filter);
        }
        
        [Fact] 
        public void RoutingTagsFilter_Filter_Node_Empty_ShouldBeExcluded()
        {
            var filter = RoutingTagsFilter.Default?.Filter?.Invoke(new Node() {
                Tags = new TagsCollection()
            });
            
            Assert.Null(filter);
        }
        
        [Fact] 
        public void RoutingTagsFilter_Filter_Way_Empty_ShouldBeExcluded()
        {
            var filter = RoutingTagsFilter.Default?.Filter?.Invoke(new Way() {
                Tags = new TagsCollection()
            });
            
            Assert.Null(filter);
        }
        
        [Fact] 
        public void RoutingTagsFilter_Filter_Relation_Empty_ShouldBeExcluded()
        {
            var filter = RoutingTagsFilter.Default?.Filter?.Invoke(new Relation() {
                Tags = new TagsCollection()
            });
            
            Assert.Null(filter);
        }
        
        [Fact] 
        public void RoutingTagsFilter_Filter_Way_Building_ShouldBeExcluded()
        {
            var filter = RoutingTagsFilter.Default?.Filter?.Invoke(new Way() {
                Tags = new TagsCollection(new Tag("building", "yes"))
            });
            
            Assert.Null(filter);
        }
        
        [Fact] 
        public void RoutingTagsFilter_Filter_Way_Highway_ShouldBeIncluded()
        {
            var filter = RoutingTagsFilter.Default?.Filter?.Invoke(new Way() {
                Tags = new TagsCollection(new Tag("highway", "residential"))
            });
            
            Assert.NotNull(filter);
        }
        
        [Fact] 
        public void RoutingTagsFilter_Filter_Way_Ferry_ShouldBeIncluded()
        {
            var filter = RoutingTagsFilter.Default?.Filter?.Invoke(new Way() {
                Tags = new TagsCollection(new Tag("route", "ferry"))
            });
            
            Assert.NotNull(filter);
        }
    }
}