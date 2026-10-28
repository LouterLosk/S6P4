using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using S6P4.Models;

namespace S6P4.Controllers
{
    public class PersonajesController : Controller
    {
        // HttpClient único y reutilizable (buena práctica: no crear uno por petición)
        private static readonly HttpClient _client = CrearCliente();

        private static HttpClient CrearCliente()
        {
            // La API usa HTTPS: .NET Framework necesita TLS 1.2
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var cliente = new HttpClient
            {
                BaseAddress = new Uri("https://rickandmortyapi.com/api/"),
                Timeout = TimeSpan.FromSeconds(30)
            };

            cliente.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            cliente.DefaultRequestHeaders.UserAgent.ParseAdd("S6P4/1.0");

            // Esta API es pública y NO requiere token.
            // Con otra API que sí lo pida, aquí agregarías:
            // cliente.DefaultRequestHeaders.Authorization =
            //     new AuthenticationHeaderValue("Bearer", "TU_TOKEN_AQUI");

            return cliente;
        }

        // GET: /Personajes?q=rick&estado=alive&genero=male&pagina=1
        public async Task<ActionResult> Index(string q = null, string estado = null, string genero = null, int pagina = 1)
        {
            if (pagina < 1) pagina = 1;
            var vm = new PersonajeListaViewModel { Q = q, Estado = estado, Genero = genero, Pagina = pagina };

            try
            {
                // Esta API cuenta las páginas desde 1 (20 personajes por página)
                string url = "character?page=" + pagina;
                if (!string.IsNullOrWhiteSpace(q)) url += "&name=" + Uri.EscapeDataString(q.Trim());
                if (!string.IsNullOrWhiteSpace(estado)) url += "&status=" + Uri.EscapeDataString(estado);
                if (!string.IsNullOrWhiteSpace(genero)) url += "&gender=" + Uri.EscapeDataString(genero);

                var resp = await _client.GetAsync(url);

                // Sin resultados para el filtro: la API responde 404 {"error":"There is nothing here"}
                if (resp.StatusCode == HttpStatusCode.NotFound)
                {
                    vm.TotalPaginas = 1;
                    return View(vm);
                }

                resp.EnsureSuccessStatusCode();

                string json = await resp.Content.ReadAsStringAsync();
                var lista = JsonConvert.DeserializeObject<PersonajeListResponse>(json);

                vm.Personajes = lista.Results ?? new List<Personaje>();
                vm.Total = lista.Info != null ? lista.Info.Count : vm.Personajes.Count;
                vm.TotalPaginas = lista.Info != null && lista.Info.Pages > 0 ? lista.Info.Pages : 1;
            }
            catch (HttpRequestException ex)
            {
                vm.Error = "No se pudo conectar con la API: " + ex.Message;
            }
            catch (TaskCanceledException)
            {
                vm.Error = "La solicitud tardó demasiado (timeout). Intenta de nuevo.";
            }
            catch (JsonException ex)
            {
                vm.Error = "Error al interpretar el JSON recibido: " + ex.Message;
            }

            return View(vm);
        }

        // GET: /Personajes/Detalle/1
        public async Task<ActionResult> Detalle(int id)
        {
            var vm = new PersonajeDetalleViewModel();

            try
            {
                var resp = await _client.GetAsync("character/" + id);

                if (!resp.IsSuccessStatusCode)
                {
                    vm.Error = "No se encontró ningún personaje con el número " + id + ".";
                    return View("~/Views/Detalle.cshtml", vm);
                }

                string json = await resp.Content.ReadAsStringAsync();
                vm.Personaje = JsonConvert.DeserializeObject<Personaje>(json);

                vm.Episodios = await ObtenerEpisodiosAsync(vm.Personaje.EpisodioIds);
            }
            catch (Exception ex)
            {
                vm.Error = "Ocurrió un error al consultar la API: " + ex.Message;
            }

            return View("~/Views/Detalle.cshtml", vm);
        }

        // Pide varios episodios en una sola llamada: episode/1,2,3
        private static async Task<List<Episodio>> ObtenerEpisodiosAsync(List<int> ids)
        {
            var lista = new List<Episodio>();
            if (ids == null || !ids.Any()) return lista;

            try
            {
                string json = await _client.GetStringAsync("episode/" + string.Join(",", ids));
                var token = JToken.Parse(json);

                // Con un solo ID la API devuelve un objeto; con varios, un arreglo
                if (token.Type == JTokenType.Array) lista = token.ToObject<List<Episodio>>();
                else lista.Add(token.ToObject<Episodio>());
            }
            catch
            {
                // los episodios son opcionales: si falla, se muestra la ficha sin ellos
            }

            return lista;
        }
    }
}