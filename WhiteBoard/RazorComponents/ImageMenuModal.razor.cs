﻿using Blazor.ModalDialog;
using Blazored.LocalStorage;
using Board.Client.Models;
using Board.Client.Services;
using Board.Client.Services.Auth;
using Board.Client.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Board.Client.RazorComponents
{
    public partial class ImageMenuModal
    {
        [Inject]
        private AppState AppState { get; set; }
        [Inject]
        private IModalDialogService ModalService { get; set; }
        [Inject]
        private IStorageClient StorageClient { get; set; }
        [Parameter]
        public ImageList ImageList { get; set; } = new();
        public ImageData SelectedImage { get; set; }
       
        
        //private void SelectCategory(ChangeEventArgs e)
        //{
        //    selectedCategory = e.Value?.ToString() ?? "";
        //}
        //private async Task GetUserImages()
        //{
        //    var user = AppState.UserName;
        //    imagesLoaded = await StorageClient.GetUserImage(user);
        //}
        //private async Task GetUserTypeImages()
        //{
        //    var user = AppState.UserName;
        //    imagesLoaded = await StorageClient.GetUserTypeImages(user, selectedCategory);
        //}
        private void SelectImage(ImageData image)
        {
            SelectedImage = image;
            var parameters = new ModalDialogParameters
            {
                {"SelectedImage",SelectedImage }
            };
            ModalService.Close(true, parameters);
        }
    }
}
