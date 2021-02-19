using System;
using Itinero.IO.Osm.Filters;
using OsmSharp;
using OsmSharp.Tags;
using Xunit;

namespace Itinero.Tests.IO.Osm.Filters
{
    public class RoutingTagsFilterTests
    {
        [Fact] 
        public void RoutingTagsFilter_Filter_Node_Null_ShouldIncluded()
        {
            var filter = RoutingTagsFilter.Filter(new Node() {
                Tags = null
            });
            
            Assert.True(filter);
        }
        
        [Fact] 
        public void RoutingTagsFilter_Filter_Way_Null_ShouldBeExcluded()
        {
            var filter = RoutingTagsFilter.Filter(new Way() {
                Tags = null
            });
            
            Assert.False(filter);
        }
        
        [Fact] 
        public void RoutingTagsFilter_Filter_Relation_Null_ShouldBeExcluded()
        {
            var filter = RoutingTagsFilter.Filter(new Relation() {
                Tags = null
            });
            
            Assert.False(filter);
        }
        
        [Fact] 
        public void RoutingTagsFilter_Filter_Node_Empty_ShouldBeIncluded()
        {
            var filter = RoutingTagsFilter.Filter(new Node() {
                Tags = new TagsCollection()
            });
            
            Assert.True(filter);
        }
        
        [Fact] 
        public void RoutingTagsFilter_Filter_Way_Empty_ShouldBeExcluded()
        {
            var filter = RoutingTagsFilter.Filter(new Way() {
                Tags = new TagsCollection()
            });
            
            Assert.False(filter);
        }
        
        [Fact] 
        public void RoutingTagsFilter_Filter_Relation_Empty_ShouldBeExcluded()
        {
            var filter = RoutingTagsFilter.Filter(new Relation() {
                Tags = new TagsCollection()
            });
            
            Assert.False(filter);
        }
        
        [Fact] 
        public void RoutingTagsFilter_Filter_Way_Building_ShouldBeExcluded()
        {
            var filter = RoutingTagsFilter.Filter(new Way() {
                Tags = new TagsCollection(new Tag("building", "yes"))
            });
            
            Assert.False(filter);
        }
        
        [Fact] 
        public void RoutingTagsFilter_Filter_Way_Highway_ShouldBeIncluded()
        {
            var filter = RoutingTagsFilter.Filter(new Way() {
                Tags = new TagsCollection(new Tag("highway", "residential"))
            });
            
            Assert.True(filter);
        }
        
        [Fact] 
        public void RoutingTagsFilter_Filter_Way_Ferry_ShouldBeIncluded()
        {
            var filter = RoutingTagsFilter.Filter(new Way() {
                Tags = new TagsCollection(new Tag("route", "ferry"))
            });
            
            Assert.True(filter);
        }
    }
}