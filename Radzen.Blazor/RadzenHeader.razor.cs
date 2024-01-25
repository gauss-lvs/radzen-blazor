using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenHeader component.
    /// </summary>
    public partial class RadzenHeader : RadzenComponentWithChildren
    {
        /// <summary>
        /// The <see cref="RadzenLayout" /> this component is nested in.
        /// </summary>
        [CascadingParameter]
        public RadzenLayout Layout { get; set; }

        /// <summary>
        /// Controls whether the CSS class "fixed" is added to the classlist.
        /// </summary>
        [Parameter]
        public bool Fixed { get; set; } = true;

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create("rz-header")
                            .Add("fixed", Layout == null && Fixed)
                            .ToString();
        }
    }
}
