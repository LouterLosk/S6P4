using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace S6P4.Models
{
    // ---------- GET https://rickandmortyapi.com/api/character?page=1&name=rick&status=alive ----------
    public class PersonajeListResponse
    {
        [JsonProperty("info")]
        public InfoPaginas Info { get; set; }

        [JsonProperty("results")]
        public List<Personaje> Results { get; set; }
    }

    public class InfoPaginas
    {
        [JsonProperty("count")] public int Count { get; set; }   // total de personajes
        [JsonProperty("pages")] public int Pages { get; set; }   // total de páginas
        [JsonProperty("next")] public string Next { get; set; }
        [JsonProperty("prev")] public string Prev { get; set; }
    }

    // ---------- GET https://rickandmortyapi.com/api/character/{id} ----------
    public class Personaje
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("status")] public string Status { get; set; }    // Alive, Dead, unknown
        [JsonProperty("species")] public string Species { get; set; }
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("gender")] public string Gender { get; set; }    // Female, Male, Genderless, unknown
        [JsonProperty("origin")] public Lugar Origin { get; set; }
        [JsonProperty("location")] public Lugar Location { get; set; }
        [JsonProperty("image")] public string Image { get; set; }
        [JsonProperty("episode")] public List<string> Episode { get; set; }   // URLs de episodios
        [JsonProperty("url")] public string Url { get; set; }
        [JsonProperty("created")] public string Created { get; set; }

        // ----- Propiedades de ayuda para la vista -----
        [JsonIgnore]
        public string EstadoClase { get { return PersonajeHelper.Slug(Status); } }

        [JsonIgnore]
        public string NumeroFormateado { get { return "#" + Id.ToString("000"); } }

        // IDs de los episodios, tomados del final de cada URL (".../episode/28" -> 28)
        [JsonIgnore]
        public List<int> EpisodioIds
        {
            get
            {
                var ids = new List<int>();
                if (Episode == null) return ids;
                foreach (var url in Episode)
                {
                    int id;
                    if (int.TryParse(url.Substring(url.LastIndexOf('/') + 1), out id)) ids.Add(id);
                }
                return ids;
            }
        }
    }

    public class Lugar
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("url")] public string Url { get; set; }
    }

    // ---------- GET https://rickandmortyapi.com/api/episode/1,2,3 ----------
    public class Episodio
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("air_date")] public string AirDate { get; set; }
        [JsonProperty("episode")] public string Codigo { get; set; }   // por ejemplo S01E01
    }

    // ---------- ViewModels ----------
    public class PersonajeListaViewModel
    {
        public List<Personaje> Personajes { get; set; } = new List<Personaje>();
        public string Q { get; set; }
        public string Estado { get; set; }
        public string Genero { get; set; }
        public int Pagina { get; set; }
        public int TotalPaginas { get; set; }
        public int Total { get; set; }
        public string Error { get; set; }
    }

    public class PersonajeDetalleViewModel
    {
        public Personaje Personaje { get; set; }
        public List<Episodio> Episodios { get; set; } = new List<Episodio>();
        public string Error { get; set; }
    }

    // ---------- Traducciones y utilidades ----------
    public static class PersonajeHelper
    {
        private static readonly Dictionary<string, string> Estados = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"Alive","Vivo"},{"Dead","Muerto"},{"unknown","Desconocido"}
        };

        private static readonly Dictionary<string, string> Generos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"Female","Femenino"},{"Male","Masculino"},{"Genderless","Sin género"},{"unknown","Desconocido"}
        };

        public static string EstadoEs(string estado)
        {
            string v;
            return estado != null && Estados.TryGetValue(estado, out v) ? v : estado;
        }

        public static string GeneroEs(string genero)
        {
            string v;
            return genero != null && Generos.TryGetValue(genero, out v) ? v : genero;
        }

        // "Alive" -> "alive" (para clases CSS)
        public static string Slug(string texto)
        {
            return string.IsNullOrEmpty(texto) ? "unknown" : texto.Trim().ToLowerInvariant();
        }
    }
}