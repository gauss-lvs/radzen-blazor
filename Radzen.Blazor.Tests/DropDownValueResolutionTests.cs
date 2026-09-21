using Bunit;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    /// <summary>
    /// Resolving the bound value against the data of a DropDown.
    /// Data assigned by a LoadData handler is a plain collection, not an EnumerableQuery, so it used to be searched with a
    /// query expression even though it sits in memory - which throws for a mismatching value type and silently finds
    /// nothing when the value property is a reference type or declared as object.
    /// </summary>
    public class DropDownValueResolutionTests
    {
        enum Kind { None = 0, First = 1, Second = 2 }

        class Key
        {
            public Key(int id) => Id = id;

            public int Id { get; }

            public override bool Equals(object obj) => obj is Key other && other.Id == Id;

            public override int GetHashCode() => Id;
        }

        class Item
        {
            public string Text { get; set; }
            public int IntId { get; set; }
            public long LongId { get; set; }
            public Kind Kind { get; set; }
            public Key Key { get; set; }
            public object Boxed { get; set; }
            public Guid Guid { get; set; }
        }

        static List<Item> Items => new()
        {
            new Item { Text = "Item 1", IntId = 1, LongId = 1, Kind = Kind.First, Key = new Key(1), Boxed = 1, Guid = Guid.Parse("11111111-1111-1111-1111-111111111111") },
            new Item { Text = "Item 2", IntId = 2, LongId = 2, Kind = Kind.Second, Key = new Key(2), Boxed = 2, Guid = Guid.Parse("22222222-2222-2222-2222-222222222222") },
        };

        /// <summary>Renders a DropDown whose data was assigned by a LoadData handler, i.e. a plain list.</summary>
        static IRenderedComponent<RadzenDropDown<TValue>> LoadedDropDown<TValue>(TestContext ctx, string valueProperty, TValue value)
        {
            return ctx.RenderComponent<RadzenDropDown<TValue>>(parameters =>
            {
                parameters.Add(p => p.TextProperty, nameof(Item.Text));
                parameters.Add(p => p.ValueProperty, valueProperty);
                parameters.Add(p => p.LoadData, args => { });
                parameters.Add(p => p.Data, Items);
                parameters.Add(p => p.Value, value);
            });
        }

        static string Label<TValue>(IRenderedComponent<RadzenDropDown<TValue>> component)
        {
            return component.Find(".rz-dropdown-label").TextContent.Trim();
        }

        [Fact]
        public void LoadedData_Resolves_MatchingValueType()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            Assert.Equal("Item 2", Label(LoadedDropDown<int>(ctx, nameof(Item.IntId), 2)));
        }

        [Fact]
        public void LoadedData_Resolves_NullableValueAgainstNonNullableProperty()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            Assert.Equal("Item 2", Label(LoadedDropDown<int?>(ctx, nameof(Item.IntId), 2)));
        }

        [Fact]
        public void LoadedData_Coerces_IntValueAgainstLongProperty()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            Assert.Equal("Item 2", Label(LoadedDropDown<int>(ctx, nameof(Item.LongId), 2)));
        }

        [Fact]
        public void LoadedData_Coerces_IntValueAgainstEnumProperty()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            Assert.Equal("Item 2", Label(LoadedDropDown<int>(ctx, nameof(Item.Kind), 2)));
        }

        [Fact]
        public void LoadedData_Resolves_ReferenceTypePropertyByEquals()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            // Equal by value, but a different instance than the one in the data.
            Assert.Equal("Item 2", Label(LoadedDropDown<Key>(ctx, nameof(Item.Key), new Key(2))));
        }

        [Fact]
        public void LoadedData_Resolves_ObjectPropertyHoldingBoxedValue()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            Assert.Equal("Item 2", Label(LoadedDropDown<object>(ctx, nameof(Item.Boxed), 2)));
        }

        [Fact]
        public void LoadedData_SelectsNothing_ForUnknownValue()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = LoadedDropDown<int>(ctx, nameof(Item.IntId), 99);

            Assert.Null(component.Instance.SelectedItem);
        }

        /// <summary>There is no conversion from string to Guid, so nothing is selected - but it must not throw.</summary>
        [Fact]
        public void LoadedData_SelectsNothing_ForUnconvertibleValue()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = LoadedDropDown<string>(ctx, nameof(Item.Guid), "22222222-2222-2222-2222-222222222222");

            Assert.Null(component.Instance.SelectedItem);
        }

        [Fact]
        public void LoadedData_Resolves_MultipleValues()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDropDown<IEnumerable<Key>>>(parameters =>
            {
                parameters.Add(p => p.TextProperty, nameof(Item.Text));
                parameters.Add(p => p.ValueProperty, nameof(Item.Key));
                parameters.Add(p => p.Multiple, true);
                parameters.Add(p => p.LoadData, args => { });
                parameters.Add(p => p.Data, Items);
                parameters.Add(p => p.Value, new[] { new Key(1), new Key(2) });
            });

            var selected = component.FindAll(".rz-state-highlight").Select(s => s.TextContent.Trim()).OrderBy(t => t).ToList();

            Assert.Equal(new[] { "Item 1", "Item 2" }, selected);
        }

        /// <summary>An IQueryable keeps the query expression so the lookup of e.g. an EF source stays server-side.</summary>
        [Fact]
        public void QueryableData_Resolves_ThroughTheQuery()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenDropDown<int>>(parameters =>
            {
                parameters.Add(p => p.TextProperty, nameof(Item.Text));
                parameters.Add(p => p.ValueProperty, nameof(Item.IntId));
                parameters.Add(p => p.Data, Items.AsQueryable());
                parameters.Add(p => p.Value, 2);
            });

            Assert.Equal("Item 2", Label(component));
        }
    }
}
