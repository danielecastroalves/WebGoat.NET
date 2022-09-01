using log4net;
using log4net.Util;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Reflection;
using System.Web;
using System.Web.Helpers;

namespace OWASP.WebGoat.NET.App_Code.DB
{
    public class MySqlDbProvider : IDbProvider
    {
        private readonly string _connectionString;
        private readonly string _host;
        private readonly string _port;
        private readonly string _pwd;
        private readonly string _uid;
        private readonly string _database;
        private readonly string _clientExec;

        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public MySqlDbProvider(ConfigFile configFile)
        {
            if (configFile == null)
                _connectionString = string.Empty;

            if (!string.IsNullOrEmpty(configFile.Get(DbConstants.KEY_PWD)))
            {
                _connectionString = string.Format("SERVER={0};PORT={1};DATABASE={2};UID={3};PWD={4}",
                                                  configFile.Get(DbConstants.KEY_HOST),
                                                  configFile.Get(DbConstants.KEY_PORT),
                                                  configFile.Get(DbConstants.KEY_DATABASE),
                                                  configFile.Get(DbConstants.KEY_UID),
                                                  configFile.Get(DbConstants.KEY_PWD));
            }
            else
            {
                _connectionString = string.Format("SERVER={0};PORT={1};DATABASE={2};UID={3}",
                                                 configFile.Get(DbConstants.KEY_HOST),
                                                 configFile.Get(DbConstants.KEY_PORT),
                                                 configFile.Get(DbConstants.KEY_DATABASE),
                                                 configFile.Get(DbConstants.KEY_UID));
            }

            _uid = configFile.Get(DbConstants.KEY_UID);
            _pwd = configFile.Get(DbConstants.KEY_PWD);
            _database = configFile.Get(DbConstants.KEY_DATABASE);
            _host = configFile.Get(DbConstants.KEY_HOST);
            _clientExec = configFile.Get(DbConstants.KEY_CLIENT_EXEC);
            _port = configFile.Get(DbConstants.KEY_PORT);
        }

        public string Name { get { return DbConstants.DB_TYPE_MYSQL; } }


        public bool TestConnection()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    string sql = "select * from information_schema.TABLES";
                    connection.Open();
                    MySqlCommand cmd = new MySqlCommand(sql, connection);
                    cmd.ExecuteNonQuery();
                    connection.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                log.Error("Error testing DB", ex);
                return false;
            }
        }

        public DataSet GetCatalogData()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    MySqlDataAdapter da = new MySqlDataAdapter("select * from Products", connection);
                    DataSet ds = new DataSet();

                    da.Fill(ds);

                    return ds;
                }
            }
            catch (SqliteException ex)
            {
                log.Error(ex.Message);
                return null;
            }
        }

        public bool RecreateGoatDb()
        {
            string args;

            if (string.IsNullOrEmpty(_pwd))
                args = string.Format("--user={0} --database={1} --host={2} --port={3} -f",
                        _uid, _database, _host, _port);
            else
                args = string.Format("--user={0} --password={1} --database={2} --host={3} --port={4} -f",
                        _uid, _pwd, _database, _host, _port);

            log.Info("Running recreate");

            int retVal1 = Math.Abs(Util.RunProcessWithInput(_clientExec, args, DbConstants.DB_CREATE_MYSQL_SCRIPT));
            int retVal2 = Math.Abs(Util.RunProcessWithInput(_clientExec, args, DbConstants.DB_LOAD_MYSQL_SCRIPT));

            return Math.Abs(retVal1) + Math.Abs(retVal2) == 0;
        }

        public bool IsValidCustomerLogin(string email, string password)
        {

            //check email/password
            string sql = "select * from CustomerLogin where email = '@email' and password = '@senha';";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    var command = new MySqlCommand(sql, connection);
                    command.Parameters.AddWithValue("@email", email);
                    command.Parameters.AddWithValue("@senha", Encoder.Encode(password));

                    MySqlDataAdapter da = new MySqlDataAdapter(command);

                    //TODO: User reader instead (for all calls)
                    DataSet ds = new DataSet();

                    da.Fill(ds);

                    return ds.Tables[0].Rows.Count != 0;
                }
            }
            catch (SqliteException ex)
            {
                //Log this and pass the ball along.
                log.Error("Error checking login", ex);

                throw new SqliteException("Error checking login", ex);
            }
        }

        //Find the bugs!
        public string CustomCustomerLogin(string email, string password)
        {
            string error_message = null;
            try
            {
                //get data
                string sql = "select * from CustomerLogin where email = '" + email + "';";

                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, connection);
                    DataSet ds = new DataSet();
                    da.Fill(ds);

                    //check if email address exists
                    if (ds.Tables[0].Rows.Count == 0)
                    {
                        error_message = "Email Address Not Found!";
                        return error_message;
                    }

                    if (!string.Equals(password.Trim(), Encoder.Decode(ds.Tables[0].Rows[0]["Password"].ToString()).Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        error_message = "Password Not Valid For This Email Address!";
                    }
                    else
                    {
                        //login successful
                        error_message = null;
                    }
                }

            }
            catch (SqliteException ex)
            {
                log.Error("Error with custom customer login", ex);
                error_message = ex.Message;
            }
            catch (Exception ex)
            {
                log.Error("Error with custom customer login", ex);
            }

            return error_message;
        }

        public string GetCustomerEmail(string customerNumber)
        {
            string output = null;
            try
            {

                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    string customerNumberParameterName = "@customerNumber";
                    string sql = $"select email from CustomerLogin where customerNumber = {customerNumberParameterName}";
                    MySqlCommand command = new MySqlCommand(sql, connection);
                    command.Parameters.AddWithValue(customerNumberParameterName, customerNumber);
                    output = command.ExecuteScalar().ToString();
                }
            }
            catch (SqliteException ex)
            {
                output = ex.Message;
            }
            return output;
        }

        public DataSet GetCustomerDetails(string customerNumber)
        {
            string sql = "select Customers.customerNumber, Customers.customerName, Customers.logoFileName, Customers.contactLastName, Customers.contactFirstName, " +
                "Customers.phone, Customers.addressLine1, Customers.addressLine2, Customers.city, Customers.state, Customers.postalCode, Customers.country, " +
                "Customers.salesRepEmployeeNumber, Customers.creditLimit, CustomerLogin.email, CustomerLogin.password, CustomerLogin.question_id, CustomerLogin.answer " +
                "From Customers, CustomerLogin where Customers.customerNumber = CustomerLogin.customerNumber and Customers.customerNumber = " + customerNumber;

            DataSet ds = new DataSet();
            try
            {

                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, connection);
                    da.Fill(ds);
                }

            }
            catch (MySqlException ex)
            {
                log.Error("Error getting customer details", ex);

                throw new SqliteException("Error getting customer details", ex);
            }
            return ds;

        }

        public DataSet GetOffice(string city)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    string sql = "select * from Offices where city = @city";
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, connection);
                    da.SelectCommand.Parameters.AddWithValue("@city", city);
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    return ds;
                }
            }
            catch (SqliteException ex)
            {
                log.Error(ex.Message);
                return null;
            }
        }

        public DataSet GetComments(string productCode)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    string sql = "select * from Comments where productCode = @productCode";
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, connection);
                    da.SelectCommand.Parameters.AddWithValue("@productCode", productCode);
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    return ds;
                }
            }
            catch (SqliteException ex)
            {
                log.Error(ex.Message);
                return null;
            }

        }

        public string AddComment(string productCode, string email, string comment)
        {
            string sql = "insert into Comments(productCode, email, comment) values (@productCode, @email, @comment);";
            string output = null;

            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    MySqlCommand command = new MySqlCommand();
                    command.Connection = connection;
                    command.CommandText = sql;
                    command.Prepare();
                    command.Parameters.AddWithValue("@productCOde", productCode);
                    command.Parameters.AddWithValue("@email", email);
                    command.Parameters.AddWithValue("@comment", comment);
                    command.ExecuteNonQuery();
                }
            }
            catch (SqliteException ex)
            {
                log.Error("Error adding comment", ex);
                output = "Error adding comment";
            }

            return output;
        }

        public string UpdateCustomerPassword(int customerNumber, string password)
        {
            string sql = "update CustomerLogin set password = '@productCOde' where customerNumber = @costumerNumber";
            string output = null;
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    MySqlCommand command = new MySqlCommand();
                    command.Connection = connection;
                    command.CommandText = sql;
                    command.Prepare();
                    command.Parameters.AddWithValue("@productCOde", Encoder.Encode(password));
                    command.Parameters.AddWithValue("@costumerNumber", customerNumber);
                    int rows_added = command.ExecuteNonQuery();

                    log.Info("Rows Added: " + rows_added + " to comment table");
                }
            }
            catch (MySqlException ex)
            {
                log.Error("Error updating customer password", ex);
                output = ex.Message;
            }

            return output;
        }

        public string[] GetSecurityQuestionAndAnswer(string email)
        {
            string sql = "select SecurityQuestions.question_text, CustomerLogin.answer from CustomerLogin, " +
                "SecurityQuestions where CustomerLogin.email = '@email' and CustomerLogin.question_id = " +
                "SecurityQuestions.question_id;";

            string[] qAndA = new string[2];

            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, connection);
                    da.SelectCommand.Parameters.AddWithValue("@email", email);

                    DataSet ds = new DataSet();
                    da.Fill(ds);

                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        DataRow row = ds.Tables[0].Rows[0];
                        qAndA[0] = HttpUtility.HtmlEncode(row[0].ToString());
                        qAndA[1] = HttpUtility.HtmlEncode(row[1].ToString());
                    }
                }
            }
            catch (SqliteException ex)
            {
                log.Error(ex.Message);
            }

            return qAndA;

        }

        public string GetPasswordByEmail(string email)
        {
            string result = string.Empty;
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    //get data
                    string sql = "select * from CustomerLogin where email = '@email';";
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, connection);
                    da.SelectCommand.Parameters.AddWithValue("@email", email);
                    DataSet ds = new DataSet();
                    da.Fill(ds);

                    //check if email address exists
                    if (ds.Tables[0].Rows.Count == 0)
                    {
                        result = "Email Address Not Found!";
                    }

                    result = Encoder.Decode(HttpUtility.UrlEncode(ds.Tables[0].Rows[0]["Password"].ToString()));
                }
            }
            catch (Exception)
            {
                result = "Operation Failed";
            }
            return result;
        }

        public DataSet GetUsers()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    string sql = "select * from CustomerLogin;";
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, connection);
                    DataSet ds = new DataSet();
                    da.Fill(ds);
                    return ds;
                }
            }
            catch (SqliteException ex)
            {
                log.Error(ex.Message);
                return null;
            }
        }

        public DataSet GetOrders(int customerID)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    string sql = "select * from Orders where customerNumber = " + customerID;
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, connection);
                    DataSet ds = new DataSet();
                    da.Fill(ds);

                    if (ds.Tables[0].Rows.Count == 0)
                        return null;
                    else
                        return ds;
                }
            }
            catch (SqliteException ex)
            {
                log.Error(ex.Message);
                return null;
            }
        }

        public DataSet GetProductDetails(string productCode)
        {
            string sql = string.Empty;
            MySqlDataAdapter da;
            DataSet ds = new DataSet();

            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    sql = "select * from Products where productCode = '" + productCode + "'";
                    da = new MySqlDataAdapter(sql, connection);
                    da.Fill(ds, "products");

                    sql = "select * from Comments where productCode = '" + productCode + "'";
                    da = new MySqlDataAdapter(sql, connection);
                    da.Fill(ds, "comments");

                    DataRelation dr = new DataRelation("prod_comments",
                    ds.Tables["products"].Columns["productCode"], //category table
                    ds.Tables["comments"].Columns["productCode"], //product table
                    false);

                    ds.Relations.Add(dr);
                    return ds;
                }
            }
            catch (SqliteException ex)
            {
                log.Error(ex.Message);
                return null;
            }
        }

        public DataSet GetOrderDetails(int orderNumber)
        {
            string sql = "select Customers.customerName, Orders.customerNumber, Orders.orderNumber, Products.productName, " +
                "OrderDetails.quantityOrdered, OrderDetails.priceEach, Products.productImage " +
                "from OrderDetails, Products, Orders, Customers where " +
                "Customers.customerNumber = Orders.customerNumber " +
                "and OrderDetails.productCode = Products.productCode " +
                "and Orders.orderNumber = OrderDetails.orderNumber " +
                "and OrderDetails.orderNumber = " + orderNumber;

            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, connection);
                    DataSet ds = new DataSet();
                    da.Fill(ds);

                    if (ds.Tables[0].Rows.Count == 0)
                        return null;
                    else
                        return ds;
                }
            }
            catch (SqliteException ex)
            {
                log.Error(ex.Message);
                return null;
            }

        }

        public DataSet GetPayments(int customerNumber)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    string sql = "select * from Payments where customerNumber = " + customerNumber;
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, connection);
                    DataSet ds = new DataSet();
                    da.Fill(ds);

                    if (ds.Tables[0].Rows.Count == 0)
                        return null;
                    else
                        return ds;
                }
            }
            catch (SqliteException ex)
            {
                log.Error(ex.Message);
                return null;
            }
        }

        public DataSet GetProductsAndCategories()
        {
            return GetProductsAndCategories(0);
        }

        public DataSet GetProductsAndCategories(int catNumber)
        {
            //TODO: Rerun the database script.
            string sql = string.Empty;
            MySqlDataAdapter da;
            DataSet ds = new DataSet();

            //catNumber is optional.  If it is greater than 0, add the clause to both statements.
            string catClause = string.Empty;
            if (catNumber >= 1)
                catClause += " where catNumber = " + catNumber;

            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {

                    sql = "select * from Categories" + catClause;
                    da = new MySqlDataAdapter(sql, connection);
                    da.Fill(ds, "categories");

                    sql = "select * from Products" + catClause;
                    da = new MySqlDataAdapter(sql, connection);
                    da.Fill(ds, "products");


                    //category / products relationship
                    DataRelation dr = new DataRelation("cat_prods",
                    HttpUtility.HtmlEncode(ds.Tables["categories"].Columns["catNumber"]), //category table
                    HttpUtility.HtmlEncode(ds.Tables["products"].Columns["catNumber"]), //product table
                    false);

                    ds.Relations.Add(dr);
                    return ds;
                }
            }
            catch (SqliteException ex)
            {
                log.Error(ex.Message);
                return null;
            }

        }

        public DataSet GetEmailByName(string name)
        {
            string sql = "select firstName, lastName, email from Employees where firstName like '" + name + "%' or lastName like '" + name + "%'";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, connection);
                    DataSet ds = new DataSet();
                    da.Fill(ds);

                    if (ds.Tables[0].Rows.Count == 0)
                        return null;
                    else
                        return ds;
                }
            }
            catch (SqliteException ex)
            {
                log.Error(ex.Message);
                return null;
            }
        }

        public string GetEmailByCustomerNumber(string num)
        {
            string output = "";
            try
            {
                var parameter = new MySqlParameter("@num", num);
                output = (string)MySqlHelper.ExecuteScalar(_connectionString, "select email from CustomerLogin where customerNumber = @num", parameter);

            }
            catch (SqliteException ex)
            {
                log.Error("Error getting email by customer number", ex);                
            }

            return output;
        }

        public DataSet GetCustomerEmails(string email)
        {
            string sql = "select email from CustomerLogin where email like '" + email + "%'";

            try
            {
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, connection);
                    DataSet ds = new DataSet();
                    da.Fill(ds);

                    if (ds.Tables[0].Rows.Count == 0)
                        return null;
                    else
                        return ds;
                }
            }
            catch (SqliteException ex)
            {
                log.Error(ex.Message);
                return null;
            }
        }
    }
}
