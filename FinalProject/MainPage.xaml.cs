using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Data.SqlClient;
using Windows.UI.Popups;
using System.Data;
using System.IO;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FinalProject
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private SqlConnection conn = new SqlConnection();
        private string conString = "Server=(local); Database=2022Exam; User=PapaDario; Password=12345";
        private SqlCommand cmd;
        private ArrayList orderList = new ArrayList();
        private DataSet menuData = new DataSet();
        private int[] itemIdList = new int[100];
        private int mealIDCount = 0;
        private int currentUID = 0;
        private Boolean isAdmin = false;
        private static readonly string[] tableNames = { "Meals", "Users", "Feedback" };
        private int tableNum = 2;
        private int numItems = 0;

        public MainPage()
        {
            this.InitializeComponent();
            refreshMenu();
        }

        //add item to the order
        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {

            int tmpItem = Int32.Parse(tbItemToAdd.Text);
            tmpItem = verifyMenuID(tmpItem);

            if (tmpItem > 0)
            {
                orderList.Add(tmpItem);
                numItems++;
                
            }


        }

        //submit completed order and print receipt
        private void btnSubmitOrder_Click(object sender, RoutedEventArgs e)
        {

            //if there are items in the order
            if (numItems > 0)
            {
                try
                {
                    conn.ConnectionString = conString;
                    cmd = conn.CreateCommand();
                    conn.Open();
                    double Total = 0.0;
                    string reciept = "Your reciept for today!\n\n";

                    //totals the value of the order, adds the items to the reciept and resets the storage variables
                    for (int i = 0; i < numItems; i++)
                    {
                        string query = "select * from Meals where MealID = " + itemIdList[i];
                        cmd.CommandText = query;
                        SqlDataReader reader = cmd.ExecuteReader();
                        double value = reader.GetDouble(3);
                        Total += value;
                        reciept += reader.GetString(1) + " ------> $" + value + "\n";


                        itemIdList[i] = 0;
                    }
                    numItems = 0;

                    //generate the last part of the reciept and create the file
                    if (currentUID > 0)
                    {
                        reciept += "\nCongratulations! you get a 10% discount for being logged in!\n" +
                            "Subtotal = $" + Total + "\n";
                        double discountedTotal = Total * 0.9;
                        reciept += "Final total = $" + discountedTotal + "\n Thanks for ordering with us! Enjoy your food!";

                        using (StreamWriter writer = new StreamWriter("reciept.txt"))
                        {
                            writer.Write(reciept);
                        }
                    }
                    else
                    {
                        reciept += "\nTotal = $" + Total + "\nThanks for ordering with us! Enjoy your food!";
                        using (StreamWriter writer = new StreamWriter("reciept.txt"))
                        {
                            writer.Write(reciept);
                        }
                    }
                }
                catch (Exception ex)
                {
                    lbSystemMsg.Text += "Error! " + ex + "\n";

                }
                finally
                {
                    cmd.Dispose();
                    conn.Close();
                }
            }
            else
            {
                lbSystemMsg.Text += "\n Order must have at least one item!\n";
            }
        }

        //checks for valid user and logs them in if its a success
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = tbUser.Text;
            string password = tbPass.Text;
            conn.ConnectionString = conString;
            cmd = conn.CreateCommand();

            try
            {
                //Makes sure username and password aren't empty
                if (!(string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)))
                {
                    string query = "select * from Users where Username = '" + username + "' and Password = '" + password + "'";
                    cmd.CommandText = query;
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        currentUID = reader.GetInt32(0);
                        if (reader.GetBoolean(3))
                        {
                            isAdmin = true;
                        }
                        else
                        {
                            isAdmin = false;
                        }
                    }

                    lbSystemMsg.Text += "\n Invalid Username/Password!\n";
                }
                else
                {
                    lbSystemMsg.Text += "\n Username/Password can't be empty!\n";
                }
            }
            catch (Exception ex)
            {
                lbSystemMsg.Text += "Error! " + ex + "\n";

            }
            finally
            {
                cmd.Dispose();
                conn.Close();
            }

        }

        //add a new user to the database
        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            string username = tbUser.Text;
            string password = tbPass.Text;
            conn.ConnectionString = conString;
            cmd = conn.CreateCommand();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                lbSystemMsg.Text += "\nUsername/Password can't be empty!\n";
            }
            else
            {
                try
                {
                    string query = "Insert into Users values('" + username + "','" + password + "'," + 0 + ")";
                    cmd.CommandText = query;
                    conn.Open();
                    cmd.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    lbSystemMsg.Text = "Failed to create new user, if this issue occurs again please contact support";

                }
                finally
                {
                    cmd.Dispose();
                    conn.Close();
                }
            }
        }

        //changes username/password for current user
        private void btnModUser_Click(object sender, RoutedEventArgs e)
        {
            string newUsername = tbUser.Text;
            string newPassword = tbPass.Text;
            if (string.IsNullOrEmpty(newUsername) || string.IsNullOrEmpty(newPassword))
            {
                lbSystemMsg.Text += "\nUsername/Password can't be empty!\n";
            }
            else if (currentUID == 0)
            {
                lbSystemMsg.Text += "\nYou must be logged in to change Username/Password!\n";
            }
            else
            {
                conn.ConnectionString = conString;
                cmd = conn.CreateCommand();
                string query = "update Users set Username = '" + newUsername + "', Password = '" + newPassword + "' where ID = " + currentUID;

                cmd.CommandText = query;
                conn.Open();
                cmd.ExecuteScalar();
            }
        }

        //submit feedback
        private void btnFeedback_Click(object sender, RoutedEventArgs e)
        {
            string feedback = tbFeedback.Text;

            if ((feedback == ""))
            {
                lbSystemMsg.Text += "Error! Feedback must have content in it\n";
            }
            else
            {
                conn.ConnectionString = conString;
                cmd = conn.CreateCommand();
                try
                {
                    string query = "Insert into Feedback values('" + feedback + "')";

                    cmd.CommandText = query;
                    conn.Open();
                    cmd.ExecuteScalar();
                    lbSystemMsg.Text += "Feedback added succesfully!\n";

                }
                catch (Exception ex)
                {
                    lbSystemMsg.Text += "Error! " + ex + "\n";

                }
                finally
                {
                    cmd.Dispose();
                    conn.Close();
                }
            }
        }

        //the next four object listners only work if logged into an admin account

        //change current table to the next one if current user is an admin
        private void btnNextTable_Click(object sender, RoutedEventArgs e)
        {
            if (isAdmin)
            {
                conn.ConnectionString = conString;
                cmd = conn.CreateCommand();

                //increments the table to move to the next one
                if (tableNum == 2)
                {
                    tableNum = 0;
                }
                else
                {
                    tableNum++;
                }

                try
                {

                    string query = "select * from " + tableNames[tableNum] + ";";
                    cmd.CommandText = query;

                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    lbAdminTable.Text = tableNames[tableNum] + " Table:\n ------------------------------------------\n";

                    //Meals table
                    if (tableNum == 0)
                    {
                        lbAdminTable.Text += ($"{ "MealID",5}{"MealName",-15} {"MealType",-15}{"MealPrice",10}\n");
                        lbColA.Text = "MealID";
                        lbColB.Text = "MealName";

                        while (reader.Read())
                        {
                            int id = (int)reader["MealID"];
                            string mealName = (string)reader["MealName"];
                            string mealType = (string)reader["MealType"];
                            decimal price = (decimal)reader["MealPrice"];
                            lbAdminTable.Text += ($"{ id,5}{mealName,-15} {mealType,-15}{price,10}\n");
                            lbColA.Text = "MealName";
                            lbColB.Text = "MealType";
                            lbColC.Text = "MealPrice";
                        }
                    }
                    else if (tableNum == 1)//Users table
                    {
                        lbAdminTable.Text += ($"{ "UserID",5}{"Username",-15} {"Password",-15}{"IsAdmin",10}\n");
                        while (reader.Read())
                        {
                            int id = (int)reader["UserID"];
                            string userName = (string)reader["Username"];
                            string password = (string)reader["Password"];
                            Boolean IsAdmin = reader.GetBoolean(3);
                            lbAdminTable.Text += ($"{ id,5}{userName,-15} {password,-15}{IsAdmin,10}\n");
                            lbColA.Text = "Username";
                            lbColB.Text = "Password";
                            lbColC.Text = "IsAdmin (true or false)";
                        }
                    }
                    else//Feedback table
                    {
                        lbAdminTable.Text += ($"{ "FeedbackID",5}{"FeedbackText",-15}\n");
                        while (reader.Read())
                        {
                            int id = (int)reader["FeedbackID"];
                            string feedbackData = (string)reader["FeedbackText"];
                            lbAdminTable.Text += ($"{ id,5}{feedbackData,-15}\n");
                            lbColA.Text = "Feedback";
                            lbColB.Text = "Column B";
                            lbColC.Text = "Column C";
                        }
                    }


                    reader.Close();

                }
                catch (Exception ex)
                {
                    lbSystemMsg.Text = "Failed to connect to " + tableNames[tableNum] + " table: " + ex.Message;

                }
                finally
                {
                    cmd.Dispose();
                    conn.Close();
                }
            }
            else
            {
                lbSystemMsg.Text = "You must be an admin to use this feature!";
            }
        }

        //add row to current table in lbAdminTable with values from lbColumnA, B, and C if current user is an admin
        private void btnCreateRow_Click(object sender, RoutedEventArgs e)
        {

            if (isAdmin)
            {
                string columnA = tbColA.Text;
                string columnB = tbColB.Text;
                string columnC = tbColC.Text;
                string query = "";

                
                try
                {
                    if (tableNum == 0)//Meals table
                    {
                        if (string.IsNullOrEmpty(columnA) || string.IsNullOrEmpty(columnB) || string.IsNullOrEmpty(columnC))
                        {
                            lbSystemMsg.Text = "\nValues must not  be empty for columns A, B, or C!\n";
                        }
                        else
                        {
                            query = "Insert into Meals values('" + columnA + "','" + columnB + "','" + columnC + "')";
                        }
                    }
                    else if (tableNum == 1)//Users table
                    {
                        if (string.IsNullOrEmpty(columnA) || string.IsNullOrEmpty(columnB) || string.IsNullOrEmpty(columnC))
                        {
                            lbSystemMsg.Text = "\nValues must not be empty for columns A, B or C!\n";
                        }
                        else
                        {
                            if (columnC.Equals("true") || columnC.Equals("false"))
                            {
                                Boolean tmpIsAdmin = Boolean.Parse(columnC);
                                query = "Insert into Meals values('" + columnA + "','" + columnB + "'," + tmpIsAdmin + ")";
                            }
                            else
                            {
                                lbSystemMsg.Text = "\nValue for IsAdmin must be true or false!\n";
                            }
                        }
                    }
                    else//Feedback table
                    {
                        if (string.IsNullOrEmpty(columnA))
                        {
                            lbSystemMsg.Text = "\nValues must not  be empty for column A!\n";
                        }
                        else
                        {
                            query = "Insert into Users values('" + columnA + "')";
                        }
                    }




                    cmd.CommandText = query;
                    conn.Open();
                    cmd.ExecuteScalar();

                }
                catch (Exception ex)
                {
                    lbSystemMsg.Text = "Failed to connect to " + tableNames[tableNum] + " table: " + ex.Message;

                }
                finally
                {
                    cmd.Dispose();
                    conn.Close();
                }


            }
            else
            {
                lbSystemMsg.Text = "You must be an admin to use this feature!";
            }
        }

        //update current table in lbAdminTable with values from lbColumnA, B, and C if current user is an admin
        private void btnUpdateRow_Click(object sender, RoutedEventArgs e)
        {
            if (isAdmin)
            {
                string columnA = tbColA.Text;
                string columnB = tbColB.Text;
                string columnC = tbColC.Text;
                int ID = Int32.Parse(tbID.Text);
                string query = "";


                try
                {
                    if (tableNum == 0)//Meals table
                    {
                        if (string.IsNullOrEmpty(columnA) || string.IsNullOrEmpty(columnB) || string.IsNullOrEmpty(columnC))
                        {
                            lbSystemMsg.Text = "\nValues must not  be empty for columns A, B, or C!\n";
                        }
                        else
                        {
                            query = "Update Meals set MealName='" + columnA + "', MealType='" + columnB + "', MealPrice'" + columnC + "' where MealID = " + ID;
                        }
                    }
                    else if (tableNum == 1)//Users table
                    {
                        if (columnC.Equals("true") || columnC.Equals("false"))
                        {
                            Boolean tmpIsAdmin = Boolean.Parse(columnC);
                            query = "Update Users set Username='" + columnA + "', Password= '" + columnB + "', IsAdmin=" + tmpIsAdmin + " Where UserID =" + ID;
                        }
                        else
                        {
                            lbSystemMsg.Text = "\nValue for IsAdmin must be true or false!\n";
                        }
                    }
                    else//Feedback table
                    {
                        if (string.IsNullOrEmpty(columnA))
                        {
                            lbSystemMsg.Text = "\nValues must not  be empty for column A!\n";
                        }
                        else
                        {
                            query = "Update Feedback set FeedbackText='" + columnA + "' where FeedbackID = " + ID;
                        }
                    }




                    cmd.CommandText = query;
                    conn.Open();
                    cmd.ExecuteScalar();

                }
                catch (Exception ex)
                {
                    lbSystemMsg.Text = "Failed to connect to " + tableNames[tableNum] + " table: " + ex.Message;

                }
                finally
                {
                    cmd.Dispose();
                    conn.Close();
                }
            }
            else
            {
                lbSystemMsg.Text = "You must be an admin to use this feature!";
            }
        }


        //delete the desired row from the current table in lbAdminTable if current user is an admin
        private void btnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (isAdmin)
            {
                int ID = Convert.ToInt32(tbID);
                conn.ConnectionString = conString;
                cmd = conn.CreateCommand();
                string query = "";
                try
                {


                    if (tableNum == 0)//Meals table
                    {
                        query = "Delete from " + tableNames[tableNum] + " where MealID = " + ID;
                    }
                    else if (tableNum == 1)//Users table
                    {
                        query = "Delete from " + tableNames[tableNum] + " where UserID = " + ID;
                    }
                    else//Feedback table
                    {
                        query = "Delete from " + tableNames[tableNum] + " where FeedbackID = " + ID;
                    }
                    cmd.CommandText = query;
                    conn.Open();
                    cmd.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    lbSystemMsg.Text = "Failed to connect to " + tableNames[tableNum] + " table: " + ex.Message; ;
                }
                finally
                {
                    cmd.Dispose();
                    conn.Close();
                }
            }
            else
            {
                lbSystemMsg.Text = "You must be an admin to use this feature!";
            }
        }


        //Non-Listener Methods

        //for checking if the submited id is an actual id
        private int verifyMenuID(int id)
        {
            if (id > 0)
            {
                for (int x = 0; x < mealIDCount; x++)
                {

                    if (itemIdList[x] == id)
                    {
                        return id;
                    }

                }
                return 0;
            }
            else
            {
                return 0;
            }

        }

        //loads/reloads the menu
        private void refreshMenu()
        {
            conn.ConnectionString = conString;
            cmd = conn.CreateCommand();


            try
            {
                //resets the list of ids
                while (mealIDCount > 0)
                {
                    itemIdList[mealIDCount - 1] = 0;
                    mealIDCount--;
                }
                mealIDCount = 0;
                string query = "select * from Meals;";
                cmd.CommandText = query;

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                lbMeals.Text = "Today's Menu:\n ------------------------------------------\n";

                while (reader.Read())
                {
                    int id = (int)reader["MealID"];
                    itemIdList[mealIDCount] = id;
                    string mealName = (string)reader["MealName"];
                    string mealType = (string)reader["MealType"];
                    decimal price = (decimal)reader["MealPrice"];
                    lbMeals.Text += ($"{ id,5}{mealName,-15} {mealType,-15}{price,10}\n");
                    mealIDCount++;
                }

                reader.Close();

            }
            catch (Exception ex)
            {
                lbSystemMsg.Text = "Failed to connect to Meals table: " + ex.Message;

            }
            finally
            {
                cmd.Dispose();
                conn.Close();
            }
        }


    }
}

