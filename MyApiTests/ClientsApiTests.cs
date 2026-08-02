using NUnit.Framework;
using RestSharp;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyApiTests
{
    [TestFixture]
    public class ClientsApiTests
    {
        // ⚠️ ВАЖНО: замените порт 7242 на ваш (посмотрите в браузере, когда запустите API)
        private const string BASE_URL = "http://localhost:5101/api/clients";
        private RestClient _client;

        [SetUp]
        public void Setup()
        {
            _client = new RestClient(BASE_URL);
        }

        [TearDown]
        public void TearDown()
        {
            _client?.Dispose();
        }

        // 1. Проверяем, что GET /api/clients возвращает 200 OK
        [Test]
        public async Task GetAllClients_ShouldReturnOk()
        {
            var request = new RestRequest();
            var response = await _client.ExecuteAsync(request);

            Assert.That((int)response.StatusCode, Is.EqualTo(200), "GET запрос должен возвращать 200 OK");
        }

        // 2. Проверяем, что POST создаёт клиента и возвращает 201
        [Test]
        public async Task CreateClient_ShouldReturnCreated_And_ReturnClientWithId()
        {
            var request = new RestRequest()
                .AddJsonBody(new
                {
                    name = "Автотест",
                    phone = "+7 111 222-33-44",
                    email = "autotest@test.com"
                });

            var response = await _client.ExecutePostAsync(request);

            Assert.That((int)response.StatusCode, Is.EqualTo(201), "POST должен возвращать 201 Created");

            var json = JsonDocument.Parse(response.Content);
            var id = json.RootElement.GetProperty("id").GetInt32();
            Assert.That(id, Is.GreaterThan(0), "ID должен быть больше 0");
        }

        // 3. Проверяем, что несуществующий ID возвращает 404
        [Test]
        public async Task GetClientById_ShouldReturnNotFound_WhenIdInvalid()
        {
            var request = new RestRequest("/999");
            var response = await _client.ExecuteAsync(request);

            Assert.That((int)response.StatusCode, Is.EqualTo(404), "Несуществующий клиент должен вернуть 404");
        }

        // 4. Проверяем, что DELETE мягко удаляет клиента
        [Test]
        public async Task DeleteClient_ShouldReturnNoContent()
        {
            // Создаём клиента
            var createRequest = new RestRequest()
                .AddJsonBody(new
                {
                    name = "Клиент для удаления",
                    phone = "+7 222 333-44-55",
                    email = "delete@test.com"
                });
            var createResponse = await _client.ExecutePostAsync(createRequest);
            var json = JsonDocument.Parse(createResponse.Content);
            var createdId = json.RootElement.GetProperty("id").GetInt32();

            // Удаляем его
            var deleteRequest = new RestRequest($"/{createdId}");
            var deleteResponse = await _client.ExecuteDeleteAsync(deleteRequest);

            Assert.That((int)deleteResponse.StatusCode, Is.EqualTo(204), "DELETE должен возвращать 204 No Content");

            // Проверяем, что его больше нет
            var getRequest = new RestRequest($"/{createdId}");
            var getResponse = await _client.ExecuteAsync(getRequest);
            Assert.That((int)getResponse.StatusCode, Is.EqualTo(404), "После удаления клиент должен быть недоступен");
        }

        // 5. Проверяем, что метод поиска по имени работает
        [Test]
        public async Task SearchByName_ShouldReturnClients_WithMatchingName()
        {
            // Создаём клиента с уникальным именем
            var uniqueName = "Иван Петров " + System.Guid.NewGuid().ToString().Substring(0, 6);
            var createRequest = new RestRequest()
                .AddJsonBody(new
                {
                    name = uniqueName,
                    phone = "+7 333 444-55-66",
                    email = "search@test.com"
                });
            await _client.ExecutePostAsync(createRequest);

            // Ищем его по имени
            var searchRequest = new RestRequest($"/search?name={uniqueName}");
            var searchResponse = await _client.ExecuteAsync(searchRequest);

            Assert.That((int)searchResponse.StatusCode, Is.EqualTo(200), "Поиск должен вернуть 200 OK");

            var json = JsonDocument.Parse(searchResponse.Content);
            var count = json.RootElement.GetArrayLength();
            Assert.That(count, Is.GreaterThan(0), "Должен найти хотя бы одного клиента");
        }

        // 6. Проверяем, что метод recent возвращает только свежих клиентов
        [Test]
        public async Task GetRecentClients_ShouldReturnOnlyRecentClients()
        {
            var request = new RestRequest("/recent?days=1");
            var response = await _client.ExecuteAsync(request);

            Assert.That((int)response.StatusCode, Is.EqualTo(200), "Recent должен возвращать 200 OK");

            // Проверяем, что все клиенты имеют дату создания за последние сутки
            var json = JsonDocument.Parse(response.Content);
            foreach (var element in json.RootElement.EnumerateArray())
            {
                var createdAt = element.GetProperty("createdAt").GetDateTime();
                Assert.That(createdAt, Is.GreaterThan(DateTime.Now.AddDays(-1)),
                    "Все клиенты должны быть созданы за последние сутки");
            }
        }
    }
}