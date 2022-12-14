using System;
using System.Data;
using Mono.Data.Sqlite;
using log4net;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Data.SqlClient;
using System.Web;
using System.Web.Helpers;
using MySql.Data.MySqlClient;

namespace OWASP.WebGoat.NET.App_Code.DB
{
    public class SqliteDbProvider : IDbProvider
    {
        private string _connectionString = string.Empty;
        private string _clientExec;
        private string _dbFileName;

        ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public string Name { get { return DbConstants.DB_TYPE_SQLITE; } }

        public SqliteDbProvider(ConfigFile configFile)
        {
            _connectionString = string.Format("Data Source={0};Version=3", configFile.Get(DbConstants.KEY_FILE_NAME));

            _clientExec = configFile.Get(DbConstants.KEY_CLIENT_EXEC);
            _dbFileName = configFile.Get(DbConstants.KEY_FILE_NAME);

            if (!File.Exists(_dbFileName))
                SqliteConnection.CreateFile(_dbFileName);
        }

        public bool TestConnection()
        {
            try
            {
                using (SqliteConnection conn = new SqliteConnection(_connectionString))
                {
                    string sql = "SELECT date('@now')";
                    conn.Open();

                    SqliteCommand command = new SqliteCommand();
                    command.Connection = conn;
                    command.CommandText = sql;
                    command.Parameters.AddWithValue("@now", DateTime.Now);

                    command.ExecuteReader();
                }
                return true;
            }
            catch (SqliteException ex)
            {
                log.Error("Error testing DB", ex);
                return false;
            }
        }

        public DataSet GetCatalogData()
        {
            try
            {
                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    SqliteDataAdapter da = new SqliteDataAdapter("select * from Products", connection);
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

        public bool IsValidCustomerLogin(string email, string password)
        {
            //check email/password
            string sql = "select * from CustomerLogin where email = '" + email + "' and password = '" +
                          Encoder.Encode(password) + "';";
            try
            {
                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    SqliteDataAdapter da = new SqliteDataAdapter(sql, connection);

                    //TODO: User reader instead (for all calls)
                    DataSet ds = new DataSet();

                    da.Fill(ds);


                    return ds.Tables[0].Rows.Count == 0;
                }
            }
            catch (SqlException ex)
            {
                //Log this and pass the ball along.
                log.Error("Error checking login", ex);

                throw new SqliteException("Error checking login", ex);
            }
        }

        public bool RecreateGoatDb()
        {
            try
            {
                log.Info("Running recreate");
                string args = string.Format("\"{0}\"", _dbFileName);
                string script = Path.Combine(Settings.RootDir, DbConstants.DB_CREATE_SQLITE_SCRIPT);
                int retVal1 = Math.Abs(Util.RunProcessWithInput(_clientExec, args, script));

                script = Path.Combine(Settings.RootDir, DbConstants.DB_LOAD_SQLITE_SCRIPT);
                int retVal2 = Math.Abs(Util.RunProcessWithInput(_clientExec, args, script));

                return Math.Abs(retVal1) + Math.Abs(retVal2) == 0;
            }
            catch (SqliteException ex)
            {
                log.Error("Error rebulding DB", ex);
                return false;
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

                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    SqliteDataAdapter da = new SqliteDataAdapter(sql, connection);
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
                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "select email from CustomerLogin where customerNumber = @customerNumber";
                    SqliteCommand command = new SqliteCommand(sql, connection);
                    command.Parameters.AddWithValue("@customerNumber", customerNumber);
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

                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    SqliteDataAdapter da = new SqliteDataAdapter(sql, connection);
                    da.Fill(ds);
                }

            }
            catch (SqliteException ex)
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
                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "select * from Offices where city = @city";
                    SqliteDataAdapter da = new SqliteDataAdapter(sql, connection);
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
                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "select * from Comments where productCode = @productCode";
                    SqliteDataAdapter da = new SqliteDataAdapter(sql, connection);
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
                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    SqliteCommand command = new SqliteCommand();
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
                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    SqliteCommand command = new SqliteCommand();
                    command.Connection = connection;
                    command.CommandText = sql;
                    command.Prepare();
                    command.Parameters.AddWithValue("@productCOde", Encoder.Encode(password));
                    command.Parameters.AddWithValue("@costumerNumber", customerNumber);

                    int rows_added = command.ExecuteNonQuery();

                    log.Info("Rows Added: " + rows_added + " to comment table");
                }
            }
            catch (SqlException ex)
            {
                log.Error("Error updating customer password", ex);
                output = ex.Message;
            }
            return output;
        }

        public string[] GetSecurityQuestionAndAnswer(string email)
        {
            string sql = "select SecurityQuestions.question_text, CustomerLogin.answer from CustomerLogin, " +
                "SecurityQuestions where CustomerLogin.email = '" + email + "' and CustomerLogin.question_id = " +
                "SecurityQuestions.question_id;";

            string[] qAndA = new string[2];

            try
            {
                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    SqliteDataAdapter da = new SqliteDataAdapter(sql, connection);

                    DataSet ds = new DataSet();
                    da.Fill(ds);

                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        DataRow row = ds.Tables[0].Rows[0];
                        qAndA[0] = row[0].ToString();
                        qAndA[1] = row[1].ToString();
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
                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    //get data
                    string sql = "select * from CustomerLogin where email = '@email';";
                    SqliteDataAdapter da = new SqliteDataAdapter(sql, connection);
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
            catch (SqliteException ex)
            {
                result = "Operation Failed";
            }

            return result;
        }

        public DataSet GetUsers()
        {
            try
            {
                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "select * from CustomerLogin;";
                    SqliteDataAdapter da = new SqliteDataAdapter(sql, connection);
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
                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "select * from Orders where customerNumber = " + customerID;
                    SqliteDataAdapter da = new SqliteDataAdapter(sql, connection);
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
            try
            {
                SqliteDataAdapter da;
                DataSet ds = new DataSet();

                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    using (SqliteCommand cmd = new SqliteCommand())
                    {
                        cmd.CommandText = "select * from Products where productCode = @productCode";
                        cmd.Parameters.AddWithValue("@productCode", productCode);
                        cmd.Connection = connection;
                        connection.Open();
                        da = new SqliteDataAdapter(cmd);
                        da.Fill(ds, "products");
                        connection.Close();
                    }

                    using (SqliteCommand cmd = new SqliteCommand())
                    {
                        cmd.CommandText = "select * from Comments where productCode = @productCode";
                        cmd.Parameters.AddWithValue("@productCode", productCode);
                        cmd.Connection = connection;
                        connection.Open();
                        da = new SqliteDataAdapter(cmd);
                        da.Fill(ds, "comments");
                        connection.Close();
                    }

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
                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    SqliteDataAdapter da = new SqliteDataAdapter(sql, connection);
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
                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "select * from Payments where customerNumber = " + customerNumber;
                    SqliteDataAdapter da = new SqliteDataAdapter(sql, connection);
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
            SqliteDataAdapter da;
            DataSet ds = new DataSet();

            //catNumber is optional.  If it is greater than 0, add the clause to both statements.
            string catClause = string.Empty;
            if (catNumber >= 1)
                catClause += " where catNumber = " + catNumber;

            try
            {
                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    sql = "select * from Categories" + catClause;
                    da = new SqliteDataAdapter(sql, connection);
                    da.Fill(ds, "categories");

                    sql = "select * from Products" + catClause;
                    da = new SqliteDataAdapter(sql, connection);
                    da.Fill(ds, "products");


                    //category / products relationship
                    DataRelation dr = new DataRelation("cat_prods",
                    ds.Tables["categories"].Columns["catNumber"], //category table
                    ds.Tables["products"].Columns["catNumber"], //product table
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
                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    SqliteDataAdapter da = new SqliteDataAdapter(sql, connection);
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

                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    string sql = "select email from CustomerLogin where customerNumber = @num";

                    connection.Open();
                    SqliteCommand command = new SqliteCommand();
                    command.Connection = connection;
                    command.CommandText = sql;
                    command.Prepare();
                    command.Parameters.AddWithValue("@num", num);

                    output = (string)command.ExecuteScalar();
                }

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
                using (SqliteConnection connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    SqliteDataAdapter da = new SqliteDataAdapter(sql, connection);
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