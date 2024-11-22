using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Prak3
{
        class Program
        {
        static string connectionString = "Server=sql.bsite.net\\MSSQL2016;Database=stepa15_;User Id=stepa15_;Password=Stepansms_2006;TrustServerCertificate=true;";

        static void Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Управление пользователями.");
                Console.WriteLine("Выберите действие:");
                Console.WriteLine("1) Посмотреть всех пользователей");
                Console.WriteLine("2) Добавить пользователя");
                Console.WriteLine("3) Обновить пользователя");
                Console.WriteLine("4) Удалить пользователя");
                Console.WriteLine("5) Выйти");

                if (int.TryParse(Console.ReadLine(), out int choice))
                {
                    switch (choice)
                    {
                        case 1: ShowAllUsers(); break;
                        case 2: AddUser(); break;
                        case 3: UpdateUser(); break;
                        case 4: DeleteUser(); break;
                        case 5: return;
                        default: Console.WriteLine("Неверный выбор."); break;
                    }
                }
                else
                {
                    Console.WriteLine("Неверный ввод.");
                }
                Console.ReadKey();
            }
        }

        static void ShowAllUsers()
        {
            Console.Clear();
            DisplayUsers("SELECT user_id, username, email, created_at, updated_at FROM [Users]");
        }

        static void AddUser()
        {
            Console.Clear();
            Console.WriteLine("Добавление пользователя:");

            string username = GetValidInput("Введите имя пользователя:", IsValidUsername);
            string password = GetValidInput("Введите пароль:", IsValidPassword);
            string email = GetValidInput("Введите email:", IsValidEmail);

            string passwordHash = HashPassword(password);

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("INSERT INTO [Users] (username, password_hash, email) VALUES (@username, @password_hash, @email)", connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.Parameters.AddWithValue("@password_hash", passwordHash);
                        command.Parameters.AddWithValue("@email", email);
                        command.ExecuteNonQuery();
                        Console.WriteLine("Пользователь успешно добавлен!");
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"Ошибка добавления: {ex.Message}");
                }
            }
            Console.ReadKey();
        }

        static void UpdateUser()
        {
            Console.Clear();
            Console.WriteLine("Обновление пользователя:");
            if (int.TryParse(GetValidInput("Введите ID пользователя:", IsValidUserID), out int userId))
            {
                string username = GetValidInput("Введите новое имя пользователя (оставьте пустым, чтобы не менять):", s => true);
                string email = GetValidInput("Введите новый email (оставьте пустым, чтобы не менять):", s => true);
                string password = GetValidInput("Введите новый пароль (оставьте пустым, чтобы не менять):", s => true);

                string updateSql = $"UPDATE [Users] SET ";
                List<string> updates = new List<string>();

                if (!string.IsNullOrEmpty(username)) updates.Add($"username = '{username}'");
                if (!string.IsNullOrEmpty(email)) updates.Add($"email = '{email}'");
                if (!string.IsNullOrEmpty(password)) updates.Add($"password_hash = '{HashPassword(password)}'");

                updateSql += string.Join(", ", updates);
                updateSql += $" WHERE user_id = {userId}";

                ExecuteSql(updateSql);
                Console.WriteLine("Пользователь успешно обновлен!");
            }
            else
            {
                Console.WriteLine("Некорректный ID пользователя.");
            }
            Console.ReadKey();
        }

        static void DeleteUser()
        {
            Console.Clear();
            Console.WriteLine("Удаление пользователя:");
            if (int.TryParse(GetValidInput("Введите ID пользователя:", IsValidUserID), out int userId))
            {
                ExecuteSql($"DELETE FROM [Users] WHERE user_id = {userId}");
                Console.WriteLine("Пользователь успешно удален!");
            }
            else
            {
                Console.WriteLine("Некорректный ID пользователя.");
            }
            Console.ReadKey();
        }


        static string GetValidInput(string prompt, Func<string, bool> validator)
        {
            string input;
            do
            {
                Console.Write($"{prompt} ");
                input = Console.ReadLine();
                if (validator(input))
                {
                    break;
                }
                Console.WriteLine("Неверный ввод. Повторите попытку.");
            } while (true);
            return input;
        }

        static bool IsValidUsername(string username) => !string.IsNullOrWhiteSpace(username);
        static bool IsValidPassword(string password) => password.Length >= 8;
        static bool IsValidEmail(string email) => !string.IsNullOrWhiteSpace(email) && email.Contains("@");
        static bool IsValidUserID(string userId) => int.TryParse(userId, out int id) && id > 0;


        static string HashPassword(string password)
        {
            byte[] salt = GenerateSalt();
            byte[] hash = GenerateHash(password, salt);
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }

        static byte[] GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }

        static byte[] GenerateHash(string password, byte[] salt)
        {
            using (var rfc2898DeriveBytes = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                return rfc2898DeriveBytes.GetBytes(256 / 8);
            }
        }

        static void DisplayUsers(string sql)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        Console.WriteLine("Список пользователей:");
                        Console.WriteLine("{0,-5} {1,-20} {2,-30} {3,-20} {4,-20}", "id", "username", "email", "created_at", "updated_at");
                        while (reader.Read())
                        {
                            Console.WriteLine("{0,-5} {1,-20} {2,-30} {3,-20} {4,-20}", reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetDateTime(3), reader.GetDateTime(4));
                        }
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"Ошибка при работе с базой данных: {ex.Message}");
                }
            }
        }

        static void ExecuteSql(string sql)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"Ошибка при работе с базой данных: {ex.Message}");
                }
            }
        }
    }
}