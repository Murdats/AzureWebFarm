﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using AzureWebFarm.Entities;
using AzureWebFarm.Storage;
using Foolproof;

namespace AzureWebFarm.ControlPanel.Areas.ControlPanel.Controllers
{
    public class WebSiteController : Controller
    {
        private readonly IWebSiteRepository _webSiteRepository;
        private readonly ISyncStatusRepository _syncStatusRepository;

        public WebSiteController(IWebSiteRepository webSiteRepository, ISyncStatusRepository syncStatusRepository)
        {
            _webSiteRepository = webSiteRepository;
            _syncStatusRepository = syncStatusRepository;
        }

        public ActionResult Detail(Guid id)
        {
            var site = _webSiteRepository.RetrieveWebSite(id);
            var bindings = _webSiteRepository.RetrieveWebSiteBindings(id).OrderBy(b => b.HostName).ThenBy(b => b.Port).ToList();
            var syncStatuses = _syncStatusRepository.RetrieveSyncStatus(site.Name);

            var model = new WebSiteDetailViewModel {Site = site, Bindings = bindings, SyncStatuses = syncStatuses};
            return View(model);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(CreateWebSiteViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var site = new WebSite
            {
                Name = model.Name,
                Description = model.Description,
                EnableCDNChildApplication = model.EnableCDNChildApplication,
                EnableTestChildApplication = model.EnableTestChildApplication
            };
            _webSiteRepository.CreateWebSite(site);
            if (model.AddStandardBindings)
            {
                var http = new Binding
                {
                    HostName = model.HostName,
                    Port = 80,
                    Protocol = "http",
                    IpAddress = "*",
                    CertificateThumbprint = ""
                };
                var https = new Binding
                {
                    HostName = model.HostName,
                    Port = 443,
                    Protocol = "https",
                    IpAddress = "*",
                    CertificateThumbprint = ""
                };
                _webSiteRepository.AddBindingToWebSite(site.Id, http);
                _webSiteRepository.AddBindingToWebSite(site.Id, https);
            }
            
            return RedirectToAction("Detail", new {area = ControlPanelAreaRegistration.Name, site.Id});
        }

        public ActionResult Edit(Guid id)
        {
            var website = _webSiteRepository.RetrieveWebSite(id);
            if (website == null)
                return HttpNotFound();

            return View(new EditWebSiteViewModel(website));
        }

        [HttpPost]
        public ActionResult Edit(EditWebSiteViewModel model)
        {
            var website = _webSiteRepository.RetrieveWebSite(model.Id);
            if (website == null)
                return HttpNotFound();

            if (!ModelState.IsValid)
                return View(model);

            website.Name = model.Name;
            website.Description = model.Description;
            website.EnableCDNChildApplication = model.EnableCDNChildApplication;
            website.EnableTestChildApplication = model.EnableTestChildApplication;
            _webSiteRepository.UpdateWebSite(website);

            return RedirectToAction("Detail", new { area = ControlPanelAreaRegistration.Name, model.Id });
        }
    }

    public class WebSiterViewModel
    {
        [Required]
        [RegularExpression("^\\w+$", ErrorMessage = "Must only contain numbers, letters or underscores.")]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        public bool EnableCDNChildApplication { get; set; }

        public bool EnableTestChildApplication { get; set; }
    }

    public class EditWebSiteViewModel : WebSiterViewModel
    {
        public EditWebSiteViewModel() {}

        public EditWebSiteViewModel(WebSite site)
        {
            Name = site.Name;
            Description = site.Description;
            EnableCDNChildApplication = site.EnableCDNChildApplication;
            EnableTestChildApplication = site.EnableTestChildApplication;
        }

        public Guid Id { get; set; }
    }

    public class CreateWebSiteViewModel : WebSiterViewModel
    {
        public bool AddStandardBindings { get; set; }

        [RequiredIf("AddStandardBindings", true)]
        [RegularExpression(@"^(([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9\-]*[a-zA-Z0-9])\.)*([A-Za-z0-9]|[A-Za-z0-9][A-Za-z0-9\-]*[A-Za-z0-9])$", ErrorMessage = "Must be a valid hostname")]
        public string HostName { get; set; }
    }

    public class WebSiteDetailViewModel
    {
        public WebSite Site { get; set; }
        public IEnumerable<Binding> Bindings { get; set; }
        public IEnumerable<SyncStatus> SyncStatuses { get; set; }
    }
}