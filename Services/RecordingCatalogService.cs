using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Recordaudio.Models;

namespace Recordaudio.Services
{
    public sealed class RecordingCatalogService
    {
        private readonly string catalogPath;

        public RecordingCatalogService()
        {
            AppPaths.EnsureDirectories();
            catalogPath = AppPaths.CatalogFilePath;
        }

        public List<RecordingItem> LoadAll()
        {
            try
            {
                if (!File.Exists(catalogPath))
                {
                    return new List<RecordingItem>();
                }

                string json = File.ReadAllText(catalogPath);
                var items = JsonSerializer.Deserialize<List<RecordingItem>>(json);

                return items ?? new List<RecordingItem>();
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur chargement catalogue : {ex.Message}");
                return new List<RecordingItem>();
            }
        }

        public void SaveAll(List<RecordingItem> items)
        {
            try
            {
                AppPaths.EnsureDirectories();

                string json = JsonSerializer.Serialize(items, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(catalogPath, json);
            }
            catch (Exception ex)
            {
                AppLogger.Log($"Erreur sauvegarde catalogue : {ex.Message}");
            }
        }

        public void Add(RecordingItem item)
        {
            var items = LoadAll();
            items.Insert(0, item);
            SaveAll(items);
        }

        public void Update(RecordingItem updated)
        {
            var items = LoadAll();

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Id == updated.Id)
                {
                    items[i] = updated;
                    SaveAll(items);
                    return;
                }
            }

            items.Insert(0, updated);
            SaveAll(items);
        }
    }
}