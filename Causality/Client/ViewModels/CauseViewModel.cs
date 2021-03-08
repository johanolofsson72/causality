﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Causality.Client.Services;
using Causality.Client.Shared;
using Causality.Shared.Models;
using Microsoft.AspNetCore.Components;
using Telerik.Blazor.Components;

namespace Causality.Client.ViewModels
{
    public class CauseViewModel : ComponentBase, ICausalityViewModel, IDisposable
    {
        #region StateProvider
        [CascadingParameter]
        public CascadingAppStateProvider StateProvider { get; set; }

        protected override void OnInitialized() => StateProvider.AppState.StateChanged += async (Source, Property) => await AppState_StateChanged(Source, Property);

        public void Dispose() => StateProvider.AppState.StateChanged -= async (Source, Property) => await AppState_StateChanged(Source, Property);

        public async Task AppState_StateChanged(ComponentBase Source, string Property)
        {
            if (Source != this)
            {
                // Inspect string Property to determine if action needs to be taken.
                // maybe we want to do something before we update the state and rerender?

                //if (Property.Equals("UseIndexedDB"))
                //{

                //}
                //else if (Property.Equals("TimeToLiveInSeconds"))
                //{

                //}
                //else if (Property.Equals("OfflineMode"))
                //{

                //}
                await InvokeAsync(StateHasChanged);
            }

            // Här ska det sparas ett object till localStorage
            await StateProvider.SaveChangesAsync();
        }
        #endregion

        [Parameter] public Int32 EventId { get; set; } = 0;
        [Parameter] public Int32 ClassId { get; set; } = 0;
        [Parameter] public String ClassKey { get; set; } = "";
        [Parameter] public EventCallback OnAdded { get; set; }
        [Parameter] public EventCallback<Dictionary<string, string>> NotifyParent { get; set; }

        [Inject] Services.CauseService CauseManager { get; set; }

        protected TelerikNotification NotificationComponent { get; set; }
        protected String Title = "Cause";
        protected List<Cause> list;
        protected Cause selectedItem;

        protected override async Task OnInitializedAsync()
        {
            await Task.Delay(0);
            GetAll();
        }

        protected async Task Delete(Int32 Id)
        {
            await CauseManager.TryDelete(Id, async (String s) => { GetAll(); Notify("success", s); }, async (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);
            await InvokeAsync(StateHasChanged);
        }

        protected async Task Update()
        {
            await CauseManager.TryUpdate(selectedItem, async (Cause m, String s) => { GetAll(); Notify("success", s); }, async (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);
            await InvokeAsync(StateHasChanged);
        }

        protected async Task Add()
        {
            Cause item = new()
            {
                EventId = EventId,
                ClassId = ClassId,
                Order = list.Count > 0 ? list.LastOrDefault().Order + 1 : 0,
                Value = "Cause",
                UpdatedDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
            };
            await CauseManager.TryInsert(item, async (Cause m, String s) => { list.Add(m); Notify("success", s); }, async (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);
            await InvokeAsync(StateHasChanged);
        }

        protected async Task Edit(Int32 Id)
        {
            await CauseManager.TryGetById(Id, "", async (Cause m, String s) => { selectedItem = m; Notify("info", s); }, async (Exception e, String r) => { selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);
        }

        protected async Task Search(ChangeEventArgs args)
        {
            if (args.Value?.ToString().Length > 0)
            {
                await CauseManager.TryGet(c => c.Value.ToLower().Contains(args.Value.ToString()), "Id", true, "", async (IEnumerable<Cause> m, String s) => { list = m.ToList(); selectedItem = null; Notify("info", s); }, async (Exception e, String r) => { list = null; selectedItem = null; Notify("error", e.ToString() + " " + r); }, StateProvider);
                await InvokeAsync(StateHasChanged);
            }
            else
            {
                GetAll();
            }
        }

        protected async void GetAll()
        {
            await CauseManager.TryGet(c => c.EventId == EventId && c.ClassId == ClassId, "Id", true, "", async (IEnumerable<Cause> m, String s) => { list = m.ToList(); selectedItem = null; Notify("info", s); }, async (Exception e, String s) => { selectedItem = null; Notify("error", e + " " + s); }, StateProvider);
        }

        protected async Task Cancel()
        {
            await Task.Delay(0);
            selectedItem = null;
        }

        protected void Notify(string theme, string text)
        {
            Dictionary<string, string> parameter = new()
            {
                { "theme", theme },
                { "text", text }
            };
            NotifyParent.InvokeAsync(parameter);
        }
    }
}
