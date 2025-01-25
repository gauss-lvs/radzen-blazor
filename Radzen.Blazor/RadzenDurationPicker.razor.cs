using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDurationPicker component.
    /// </summary>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenDurationPicker @bind-Value=@someValue TValue="TimeSpan" Change=@(args => Console.WriteLine($"Selected timespan: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenDurationPicker<TValue> : RadzenComponent, IRadzenFormComponent
    {
        /// <summary>
        /// Specifies additional custom attributes that will be rendered by the input.
        /// </summary>
        /// <value>The attributes.</value>
        [Parameter]
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

        int? hour;

        void OnUpdateHourInput(ChangeEventArgs args)
        {
            var value = $"{args.Value}";
            if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            {
                hour = null;
                return;
            }

            hour = Math.Max(v, 0);
        }

        int? minutes;

        void OnUpdateHourMinutes(ChangeEventArgs args)
        {
            var value = $"{args.Value}";
            if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            {
                minutes = null;
                return;
            }

            minutes = Math.Max(Math.Min(v, 59), 0);
        }

        int? seconds;

        void OnUpdateHourSeconds(ChangeEventArgs args)
        {
            var value = $"{args.Value}";
            if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            {
                seconds = null;
                return;
            }

            seconds = Math.Max(Math.Min(v, 59), 0);
        }

        async Task UpdateValueFromTime(TimeSpan newValue)
        {
            if (ShowTimeOkButton)
            {
                CurrentTimeSpan = newValue;
            }
            else
            {
                Value = newValue;
                CurrentTimeSpan = newValue;
                await OnChange();
            }
        }

        async Task UpdateHour(int v)
        {
            var newValue = new TimeSpan(0, v, CurrentTimeSpan.Minutes, CurrentTimeSpan.Seconds);

            hour = (int)newValue.TotalHours;
            await UpdateValueFromTime(newValue);
        }

        async Task UpdateMinutes(int v)
        {
            var newValue = new TimeSpan(CurrentTimeSpan.Days, CurrentTimeSpan.Hours, v, CurrentTimeSpan.Seconds);

            minutes = newValue.Minutes;
            await UpdateValueFromTime(newValue);
        }

        async Task UpdateSeconds(int v)
        {
            var newValue = new TimeSpan(CurrentTimeSpan.Days, CurrentTimeSpan.Hours, CurrentTimeSpan.Minutes, v);

            seconds = newValue.Seconds;
            await UpdateValueFromTime(newValue);
        }

        async Task OkClick()
        {
            if (PopupRenderMode == PopupRenderMode.OnDemand && !Disabled && !ReadOnly && !Inline)
            {
                await popup.CloseAsync(Element);
            }

            if (Min.HasValue && CurrentTimeSpan < Min.Value || Max.HasValue && CurrentTimeSpan > Max.Value)
            {
                return;
            }

            if (!Disabled)
            {
                TimeSpan timeSpan = CurrentTimeSpan;

                if (CurrentTimeSpan.TotalHours != hour && hour != null)
                {
                    timeSpan = new TimeSpan(0, hour.Value, CurrentTimeSpan.Minutes, CurrentTimeSpan.Seconds);
                }

                if (CurrentTimeSpan.Minutes != minutes && minutes != null)
                {
                    timeSpan = new TimeSpan(CurrentTimeSpan.Days, CurrentTimeSpan.Hours, minutes.Value, CurrentTimeSpan.Seconds);
                }

                if (CurrentTimeSpan.Seconds != seconds && seconds != null)
                {
                    timeSpan = new TimeSpan(CurrentTimeSpan.Days, CurrentTimeSpan.Hours, CurrentTimeSpan.Minutes, seconds.Value);
                }

                Value = timeSpan;

                await OnChange();
            }
        }

        class NameValue
        {
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>The name.</value>
            public string Name { get; set; }
            /// <summary>
            /// Gets or sets the value.
            /// </summary>
            /// <value>The value.</value>
            public int Value { get; set; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether value can be cleared.
        /// </summary>
        /// <value><c>true</c> if value can be cleared; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowClear { get; set; }

        /// <summary>
        /// Gets or sets the tab index.
        /// </summary>
        /// <value>The tab index.</value>
        [Parameter]
        public int TabIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets the name of the form component.
        /// </summary>
        /// <value>The name.</value>
        [Parameter]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the input CSS class.
        /// </summary>
        /// <value>The input CSS class.</value>
        [Parameter]
        public string InputClass { get; set; }

        /// <summary>
        /// Gets or sets the Minimum Selectable TimeSpan.
        /// </summary>
        /// <value>The Minimum Selectable TimeSpan.</value>
        [Parameter]
        public TimeSpan? Min { get; set; }

        /// <summary>
        /// Gets or sets the Maximum Selectable TimeSpan.
        /// </summary>
        /// <value>The Maximum Selectable TimeSpan.</value>
        [Parameter]
        public TimeSpan? Max { get; set; }

        /// <summary>
        /// Gets or sets the Initial TimeSpan View.
        /// </summary>
        /// <value>The Initial TimeSpan View.</value>
        [Parameter]
        public TimeSpan? InitialViewTimeSpan { get; set; }

        TimeSpan? _timeSpanValue;

        TimeSpan? TimeSpanValue
        {
            get
            {
                return _timeSpanValue;
            }
            set
            {
                if (_timeSpanValue != value)
                {
                    _timeSpanValue = value;
                    Value = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the timespan render callback. Use it to set attributes.
        /// </summary>
        /// <value>The timespan render callback.</value>
        [Parameter]
        public Action<DurationRenderEventArgs> TimeSpanRender { get; set; }

        DurationRenderEventArgs DurationAttributes(TimeSpan value)
        {
            var args = new DurationRenderEventArgs() { Duration = value, Disabled = (Min.HasValue && value < Min.Value) || (Max.HasValue && value > Max.Value) };

            if (TimeSpanRender != null)
            {
                TimeSpanRender(args);
            }

            return args;
        }

        object _value;

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (_value != value)
                {
                    _value = ConvertToTValue(value);
                    _currentTimeSpan = default(TimeSpan);

                    if (value is TimeSpan timeSpan && timeSpan != default(TimeSpan))
                    {
                        TimeSpanValue = timeSpan;
                    }
                    else
                    {
                        TimeSpanValue = null;
                    }

                }
            }
        }

        private static object ConvertToTValue(object value)
        {
            return value;
        }

        TimeSpan _currentTimeSpan;

        private TimeSpan CurrentTimeSpan
        {
            get
            {
                if (_currentTimeSpan == default(TimeSpan))
                {
                    _currentTimeSpan = HasValue ? TimeSpanValue.Value : InitialViewTimeSpan ?? default(TimeSpan);
                }
                return _currentTimeSpan;
            }
            set
            {
                _currentTimeSpan = value;
                CurrentTimeSpanChanged.InvokeAsync(value);
            }
        }

        /// <summary>
        /// Gets or set the current timespan changed callback.
        /// </summary>
        [Parameter]
        public EventCallback<TimeSpan> CurrentTimeSpanChanged { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is bound (ValueChanged callback has delegate).
        /// </summary>
        /// <value><c>true</c> if this instance is bound; otherwise, <c>false</c>.</value>
        public bool IsBound
        {
            get
            {
                return ValueChanged.HasDelegate;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has value.
        /// </summary>
        /// <value><c>true</c> if this instance has value; otherwise, <c>false</c>.</value>
        public bool HasValue
        {
            get
            {
                return TimeSpanValue.HasValue && TimeSpanValue != default(TimeSpan);
            }
        }

        /// <summary>
        /// Gets the formatted value.
        /// </summary>
        /// <value>The formatted value.</value>
        public string FormattedValue
        {
            get
            {
                if (HasValue)
                {
                    if (TimeFormat.ToLowerInvariant().EndsWith(":ss"))
                        return (int)TimeSpanValue?.TotalHours + TimeSpanValue?.ToString(@"\:mm\:ss");
                    else
                        return (int)TimeSpanValue?.TotalHours + TimeSpanValue?.ToString(@"\:mm");
                }
                return "";
            }
        }

        IRadzenForm _form;

        /// <summary>
        /// Gets or sets the form.
        /// </summary>
        /// <value>The form.</value>
        [CascadingParameter]
        public IRadzenForm Form
        {
            get
            {
                return _form;
            }
            set
            {
                if (_form != value && value != null)
                {
                    _form = value;
                    _form.AddComponent(this);
                }
            }
        }

        /// <summary>
        /// Gets input reference.
        /// </summary>
        protected ElementReference input;

        /// <summary>
        /// Parses the timespan.
        /// </summary>
        protected async Task ParseTimeSpan()
        {
            TimeSpan? newValue;
            var inputValue = await JSRuntime.InvokeAsync<string>("Radzen.getInputValue", input);
            bool valid = TryParseInput(inputValue, out TimeSpan value);

            var nullable = Nullable.GetUnderlyingType(typeof(TValue)) != null || AllowClear;

            if (valid && !DurationAttributes(value).Disabled)
            {
                newValue = CurrentTimeSpan != default(TimeSpan) ? new TimeSpan(value.Days, value.Hours, value.Minutes, value.Seconds) : value;
            }
            else
            {
                newValue = null;

                if (nullable)
                {
                    await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", input, "");
                }
                else
                {
                    await JSRuntime.InvokeAsync<string>("Radzen.setInputValue", input, FormattedValue);
                }

            }

            if (TimeSpanValue != newValue && (newValue != null || nullable))
            {
                TimeSpanValue = newValue;
                await ValueChanged.InvokeAsync((TValue)Value);

                if (FieldIdentifier.FieldName != null)
                {
                    EditContext?.NotifyFieldChanged(FieldIdentifier);
                }

                await Change.InvokeAsync(TimeSpanValue);
                StateHasChanged();
            }
        }

        /// <summary>
        /// Parse the input using an function outside the Radzen-library
        /// </summary>
        [Parameter]
        public Func<string, TimeSpan?> ParseInput { get; set; }

        private bool TryParseInput(string inputValue, out TimeSpan value)
        {
            value = TimeSpan.Zero;
            bool valid = false;

            if (ParseInput != null)
            {
                TimeSpan? custom = ParseInput.Invoke(inputValue);

                if (custom.HasValue)
                {
                    valid = true;
                    value = custom.Value;
                }
            }
            else
            {
                valid = TimeSpan.TryParseExact(inputValue, TimeFormat, null, TimeSpanStyles.None, out value);

                if (!valid)
                {
                    valid = TimeSpan.TryParse(inputValue, out value);
                }
            }

            return valid;
        }

        async Task Clear()
        {
            if (Disabled || ReadOnly)
                return;

            Value = null;

            await ValueChanged.InvokeAsync(default(TValue));

            if (FieldIdentifier.FieldName != null)
            {
                EditContext?.NotifyFieldChanged(FieldIdentifier);
            }

            await Change.InvokeAsync(TimeSpanValue);
            StateHasChanged();
        }

        private string ButtonClasses
        {
            get => $"rz-button-icon-left rzi rzi-time";
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDurationPicker{TValue}"/> is inline - only Calender.
        /// </summary>
        /// <value><c>true</c> if inline; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Inline { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether read only.
        /// </summary>
        /// <value><c>true</c> if read only; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether input is allowed.
        /// </summary>
        /// <value><c>true</c> if input is allowed; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool AllowInput { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether popup durationpicker button is shown.
        /// </summary>
        /// <value><c>true</c> if need show button open durationpicker popup; <c>false</c> if need hide button, click for input field open durationpicker popup.</value>
        [Parameter]
        public bool ShowButton { get; set; } = true;

        private bool IsReadonly => ReadOnly || !AllowInput;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenDurationPicker{TValue}"/> is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets the FormFieldContext of the component
        /// </summary>
        public IFormFieldContext FormFieldContext { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether days part is shown.
        /// </summary>
        /// <value><c>true</c> if days part is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowDays { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether time part is shown.
        /// </summary>
        /// <value><c>true</c> if time part is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether seconds are shown.
        /// </summary>
        /// <value><c>true</c> if seconds are shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowSeconds { get; set; }

        /// <summary>
        /// Gets or sets the hours step.
        /// </summary>
        /// <value>The hours step.</value>
        [Parameter]
        public string HoursStep { get; set; }

        /// <summary>
        /// Gets or sets the minutes step.
        /// </summary>
        /// <value>The minutes step.</value>
        [Parameter]
        public string MinutesStep { get; set; }

        /// <summary>
        /// Gets or sets the seconds step.
        /// </summary>
        /// <value>The seconds step.</value>
        [Parameter]
        public string SecondsStep { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the hour picker is padded with a leading zero.
        /// </summary>
        /// <value><c>true</c> if hour component is padded; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool PadHours { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the minute picker is padded with a leading zero.
        /// </summary>
        /// <value><c>true</c> if hour component is padded; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool PadMinutes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the second picker is padded with a leading zero.
        /// </summary>
        /// <value><c>true</c> if hour component is padded; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool PadSeconds { get; set; }

        enum StepType
        {
            /// <summary>
            /// The hours
            /// </summary>
            Hours,
            /// <summary>
            /// The minutes
            /// </summary>
            Minutes,
            /// <summary>
            /// The seconds
            /// </summary>
            Seconds
        }

        double getStep(StepType type)
        {
            double step = 1;

            if (type == StepType.Hours)
            {
                step = parseStep(HoursStep);
            }
            else if (type == StepType.Minutes)
            {
                step = parseStep(MinutesStep);
            }
            else if (type == StepType.Seconds)
            {
                step = parseStep(SecondsStep);
            }

            return step;
        }

        double parseStep(string step)
        {
            return string.IsNullOrEmpty(step) || step == "any" ? 1 : double.Parse(step.Replace(",", "."), CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets or sets a value indicating whether time ok button is shown.
        /// </summary>
        /// <value><c>true</c> if time ok button is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowTimeOkButton { get; set; } = true;

        /// <summary>
        /// Gets or sets the timespan format.
        /// </summary>
        /// <value>The timespan format.</value>
        [Parameter]
        public string TimeFormat { get; set; }

        /// <summary>
        /// Gets or sets the input placeholder.
        /// </summary>
        /// <value>The input placeholder.</value>
        [Parameter]
        public string Placeholder { get; set; }

        /// <summary>
        /// Gets or sets the change callback.
        /// </summary>
        /// <value>The change callback.</value>
        [Parameter]
        public EventCallback<TimeSpan?> Change { get; set; }

        /// <summary>
        /// Gets or sets the value changed callback.
        /// </summary>
        /// <value>The value changed callback.</value>
        [Parameter]
        public EventCallback<TValue> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the footer template.
        /// </summary>
        /// <value>The footer template.</value>
        [Parameter]
        public RenderFragment FooterTemplate { get; set; }

        string contentStyle = "display:none;";

        private string getStyle()
        {
            return $"display: inline-block;{(Inline ? "overflow:auto;" : "")}{(Style != null ? Style : "")}";
        }

        /// <summary>
        /// Closes this instance popup.
        /// </summary>
        public void Close()
        {
            if (PopupRenderMode == PopupRenderMode.OnDemand && !Disabled && !ReadOnly && !Inline)
            {
                InvokeAsync(() => popup.CloseAsync(Element));
            }

            if (!Disabled)
            {
                contentStyle = "display:none;";
                StateHasChanged();
            }
        }

        private string PopupStyle
        {
            get
            {
                if (Inline)
                {
                    return "white-space: nowrap";
                }
                else
                {
                    return $"width: 320px; {contentStyle}";
                }
            }
        }

        async Task OnChange()
        {
            await ValueChanged.InvokeAsync((TValue)Value);

            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(TimeSpanValue);

        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return ClassList.Create()
                            .Add("rz-calendar-inline", Inline)
                            .Add(FieldIdentifier, EditContext)
                            .ToString();
        }

        private async Task SetDay(TimeSpan newValue)
        {
            if (ShowTimeOkButton)
            {
                CurrentTimeSpan = new TimeSpan(newValue.Days, CurrentTimeSpan.Hours, CurrentTimeSpan.Minutes, CurrentTimeSpan.Seconds);
                await OkClick();
            }
            else
            {
                var v = new TimeSpan(newValue.Days, CurrentTimeSpan.Hours, CurrentTimeSpan.Minutes, CurrentTimeSpan.Seconds);
                if (v != TimeSpanValue)
                {
                    TimeSpanValue = v;
                    await OnChange();
                    Close();
                }
            }
        }

        private string getOpenPopup()
        {
            return PopupRenderMode == PopupRenderMode.Initial && !Disabled && !ReadOnly && !Inline ? $"Radzen.togglePopup(this.parentNode, '{PopupID}')" : "";
        }

        private string getOpenPopupForInput()
        {
            return PopupRenderMode == PopupRenderMode.Initial && !Disabled && !ReadOnly && !Inline && (!AllowInput || !ShowButton) ? $"Radzen.togglePopup(this.parentNode, '{PopupID}')" : "";
        }

        /// <summary>
        /// Gets or sets the edit context.
        /// </summary>
        /// <value>The edit context.</value>
        [CascadingParameter]
        public EditContext EditContext { get; set; }

        /// <summary>
        /// Gets the field identifier.
        /// </summary>
        /// <value>The field identifier.</value>
        public FieldIdentifier FieldIdentifier { get; private set; }

        /// <summary>
        /// Gets or sets the value expression.
        /// </summary>
        /// <value>The value expression.</value>
        [Parameter]
        public Expression<Func<TValue>> ValueExpression { get; set; }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var shouldClose = false;

            if (parameters.DidParameterChange(nameof(Visible), Visible))
            {
                var visible = parameters.GetValueOrDefault<bool>(nameof(Visible));
                shouldClose = !visible;
            }

            await base.SetParametersAsync(parameters);

            if (shouldClose && !firstRender)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", PopupID);
            }

            if (EditContext != null && ValueExpression != null && FieldIdentifier.Model != EditContext.Model)
            {
                FieldIdentifier = FieldIdentifier.Create(ValueExpression);
                EditContext.OnValidationStateChanged -= ValidationStateChanged;
                EditContext.OnValidationStateChanged += ValidationStateChanged;
            }
        }

        private void ValidationStateChanged(object sender, ValidationStateChangedEventArgs e)
        {
            StateHasChanged();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();

            if (EditContext != null)
            {
                EditContext.OnValidationStateChanged -= ValidationStateChanged;
            }

            Form?.RemoveComponent(this);

            if (IsJSRuntimeAvailable)
            {
                JSRuntime.InvokeVoidAsync("Radzen.destroyPopup", PopupID);
            }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <returns>System.Object.</returns>
        public object GetValue()
        {
            return Value;
        }

        private string PopupID
        {
            get
            {
                return $"popup{UniqueID}";
            }
        }

        private bool firstRender = true;

        /// <summary>
        /// Called when [after render asynchronous].
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> [first render].</param>
        /// <returns>Task.</returns>
        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            this.firstRender = firstRender;

            return base.OnAfterRenderAsync(firstRender);
        }

        Popup popup;

        /// <summary>
        /// Gets or sets the render mode.
        /// </summary>
        /// <value>The render mode.</value>
        [Parameter]
        public PopupRenderMode PopupRenderMode { get; set; } = PopupRenderMode.Initial;

        async Task OnToggle()
        {
            if (PopupRenderMode == PopupRenderMode.OnDemand && !Disabled && !ReadOnly && !Inline)
            {
                await popup.ToggleAsync(Element);
#if NET5_0_OR_GREATER
                await FocusAsync();
#endif
            }
        }

#if NET5_0_OR_GREATER
        /// <inheritdoc/>
        public async ValueTask FocusAsync()
        {
            await input.FocusAsync();
        }
#endif
    }
}
