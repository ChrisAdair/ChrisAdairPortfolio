using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using UnityEngine;

namespace Barebones.MasterServer
{
    public class AuthDbMysql : IAuthDatabase
    {
        private string _connectionString;

        public AuthDbMysql(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IAccountData CreateAccountObject()
        {
            return new SqlAccountData();
        }

        public void GetAccount(string username, Action<IAccountData> callback)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "SELECT * FROM accounts WHERE username = @username;";
                cmd.Parameters.AddWithValue("@username", username);

                var reader = cmd.ExecuteReader();

                // There's no such user
                if (!reader.HasRows)
                    callback.Invoke(null);

                callback.Invoke(ReadAccountData(reader, cmd));
            }
        }

        public void GetAccountByToken(string token, Action<IAccountData> callback)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "SELECT * FROM accounts WHERE token = @token;";
                cmd.Parameters.AddWithValue("@token", token);

                var reader = cmd.ExecuteReader();

                // There's no such user
                if (!reader.HasRows)
                    callback.Invoke(null);

                callback.Invoke(ReadAccountData(reader, cmd));
            }
        }

        public void GetAccountByEmail(string email, Action<IAccountData> callback)
        {

            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "SELECT * FROM accounts WHERE email = @email;";
                cmd.Parameters.AddWithValue("@email", email);

                var reader = cmd.ExecuteReader();

                // There's no such user
                if (!reader.HasRows)
                    callback.Invoke(null);

                callback.Invoke(ReadAccountData(reader, cmd));
            }
        }

        public void SavePasswordResetCode(IAccountData account, string code, Action doneCallback)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "INSERT INTO password_reset_codes (email, code) " +
                                  "VALUES(@email, @code) " +
                                  "ON DUPLICATE KEY UPDATE code = @code";
                cmd.CommandTimeout = 0;
                cmd.Parameters.AddWithValue("@email", account.Email);
                cmd.Parameters.AddWithValue("@code", code);
                cmd.ExecuteNonQuery();
            }

            doneCallback.Invoke();
        }

        public void GetPasswordResetData(string email, Action<IPasswordResetData> callback)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "SELECT * FROM password_reset_codes " +
                                  "WHERE email = @email";
                cmd.Parameters.AddWithValue("@email", email);

                var reader = cmd.ExecuteReader();

                // There's no such user
                if (!reader.HasRows)
                    callback.Invoke(null);

                // Read row
                reader.Read();

                callback.Invoke(new PasswordResetData()
                {
                    Code = reader["code"] as string,
                    Email = reader["email"] as string
                });
            }
        }

        public void SaveEmailConfirmationCode(string email, string code, Action doneCallback)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "INSERT INTO email_confirmation_codes (email, code) " +
                                  "VALUES(@email, @code) " +
                                  "ON DUPLICATE KEY UPDATE code = @code";

                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@code", code);
                cmd.CommandTimeout = 0;
                cmd.ExecuteNonQuery();
            }

            doneCallback.Invoke();
        }

        public void GetEmailConfirmationCode(string email, Action<string> callback)
        {
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "SELECT * FROM email_confirmation_codes " +
                                  "WHERE email = @email";
                cmd.Parameters.AddWithValue("@email", email);

                var reader = cmd.ExecuteReader();

                // There's no such user
                if (!reader.HasRows)
                    callback.Invoke(null);

                // Read row
                reader.Read();

                callback.Invoke(reader["code"] as string);
            }
        }

        public void UpdateAccount(IAccountData acc, Action doneCallback)
        {
            var account = acc as SqlAccountData;
            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "UPDATE accounts " +
                                  "SET password = @password, " +
                                  "email = @email, " +
                                  "is_admin = @is_admin, " +
                                  "is_guest = @is_guest, " +
                                  "is_email_confirmed = @is_email_confirmed " +
                                  "WHERE account_id = @account_id";
                cmd.Parameters.AddWithValue("@password", account.Password);
                cmd.Parameters.AddWithValue("@email", account.Email);
                cmd.Parameters.AddWithValue("@is_admin", account.IsAdmin ? 1 : 0);
                cmd.Parameters.AddWithValue("@is_guest", account.IsGuest ? 1 : 0);
                cmd.Parameters.AddWithValue("@is_email_confirmed", account.IsEmailConfirmed ? 1 : 0);
                cmd.Parameters.AddWithValue("@account_id", account.AccountId);
                cmd.CommandTimeout = 0;
                cmd.ExecuteNonQuery();
            }

            doneCallback.Invoke();
        }

        public void InsertNewAccount(IAccountData acc, Action doneCallback)
        {
            var account = acc as SqlAccountData;

            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "INSERT INTO accounts " +
                                  "SET username = @username, " +
                                  "password = @password, " +
                                  "email = @email, " +
                                  "is_admin = @is_admin, " +
                                  "is_guest = @is_guest, " +
                                  "is_email_confirmed = @is_email_confirmed ";

                cmd.Parameters.AddWithValue("@username", account.Username);
                cmd.Parameters.AddWithValue("@password", account.Password);
                cmd.Parameters.AddWithValue("@email", account.Email);
                cmd.Parameters.AddWithValue("@is_admin", account.IsAdmin ? 1 : 0);
                cmd.Parameters.AddWithValue("@is_guest", account.IsGuest ? 1 : 0);
                cmd.Parameters.AddWithValue("@is_email_confirmed", account.IsEmailConfirmed ? 1 : 0);
                cmd.CommandTimeout = 0;
                cmd.ExecuteNonQuery();
                account.AccountId = (int)cmd.LastInsertedId;
            }

            doneCallback.Invoke();
        }

        public void InsertToken(IAccountData acc, string token, Action doneCallback)
        {
            var account = acc as SqlAccountData;

            using (var con = new MySqlConnection(_connectionString))
            using (var cmd = new MySqlCommand())
            {
                con.Open();

                cmd.Connection = con;
                cmd.CommandText = "UPDATE accounts " +
                                  "SET token = @token " +
                                  "WHERE account_id = @account_id";
                cmd.Parameters.AddWithValue("@token", token);
                cmd.Parameters.AddWithValue("@account_id", account.AccountId);

                account.Token = token;
                cmd.CommandTimeout = 0;
                cmd.ExecuteNonQuery();
            }
            doneCallback.Invoke();
        }

        private SqlAccountData ReadAccountData(MySqlDataReader reader, MySqlCommand cmd)
        {
            SqlAccountData account = null;

            // Read primary account data
            while (reader.Read())
            {
                account = new SqlAccountData()
                {
                    Username = reader["username"] as string,
                    AccountId = reader.GetInt32("account_id"),
                    Email = reader["email"] as string,
                    Password = reader["password"] as string,
                    IsAdmin = reader["is_admin"] as bool? ?? false,
                    IsGuest = reader["is_guest"] as bool? ?? false,
                    IsEmailConfirmed = reader["is_email_confirmed"] as bool? ?? false,
                    Properties = new Dictionary<string, string>(),
                    Token = reader["token"] as string
                };
            }

            if (account == null)
                return null;

            // Read account values
            reader.Close();

            cmd.CommandText = "SELECT * FROM account_properties WHERE account_id = @account_id";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@account_id", account.AccountId);
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var key = reader["prop_key"] as string ?? "";
                var value = reader["prop_val"] as string ?? "";

                if (string.IsNullOrEmpty(key))
                    continue;

                account.Properties[key] = value;
            }
            return account;
        }

        public class SqlAccountData : IAccountData
        {
            public int AccountId { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string Email { get; set; }
            public string Token { get; set; }
            public bool IsAdmin { get; set; }
            public bool IsGuest { get; set; }

            public bool IsEmailConfirmed { get; set; }

            public Dictionary<string, string> Properties { get; set; }

            public event Action<IAccountData> OnChange;

            public void MarkAsDirty()
            {
                if (OnChange != null)
                    OnChange.Invoke(this);
            }
        }

        public class PasswordResetData : IPasswordResetData
        {
            public string Email { get; set; }
            public string Code { get; set; }
        }
    }
}