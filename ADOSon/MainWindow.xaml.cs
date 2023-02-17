using System;
using System.Data;
using System.Data.SqlClient;
using System.Reflection.PortableExecutable;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ADOSon;

public partial class MainWindow : Window
{
    string? conStr = string.Empty;
    SqlConnection? conn = null;
    SqlDataReader? reader = null;
    DataTable? table = null;
    DataSet? dataset = null;
    SqlDataAdapter? adapter = null;
    public MainWindow()
    {
        InitializeComponent();
        conStr = System.Configuration.ConfigurationManager.AppSettings["ConnectionString"];
        RetrievevProducts();
    }

    private async void RetrievevProducts()
    {
        conn = new SqlConnection(conStr);
        try
        {
            await conn.OpenAsync();
            string SelectCommand = "SELECT Id, Products.Name, Price, Products.Quantity FROM Products";

            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "WAITFOR DELAY '00:00:03';";
            cmd.CommandText += SelectCommand;
            table = new DataTable();

            adapter = new SqlDataAdapter(SelectCommand, conn);
            dataset = new DataSet();
            adapter.Fill(dataset);
            reader = await cmd.ExecuteReaderAsync();

            int line = 0;
            do
            {
                while (await reader.ReadAsync())
                {
                    if (line == 0)
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            table.Columns.Add(reader.GetName(i));
                        }
                        line++;
                    }
                    DataRow row = table.NewRow();

                    for (int i = 0; i < reader.FieldCount; i++)
                        row[i] = await reader.GetFieldValueAsync<object>(i);


                    table.Rows.Add(row);

                }
            } while (reader.NextResult());

            DataGridView.ItemsSource = null;
            DataGridView.ItemsSource = table.AsDataView();
        } 
        catch(Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            await conn.CloseAsync();
            await reader.CloseAsync();
        }
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        DataRowView rowView = DataGridView.SelectedItem as DataRowView;
        if (rowView is not null)
        {
            DataRow row = rowView.Row;
            string Id = row[0].ToString();
            using (SqlConnection conn = new SqlConnection(conStr))
            {
                await conn.OpenAsync();
                SqlCommand cmd = new SqlCommand("DELETE FROM Products WHERE Id = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", Id);
                int result = await cmd.ExecuteNonQueryAsync();
                if (result > 0)
                    row.Delete();
            }
        }
    }

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        SqlCommand updateCM = new SqlCommand()
        {
            CommandText = "usp_UpdateProducts",
            Connection = conn,
            CommandType = CommandType.StoredProcedure,
        };

        updateCM.Parameters.Add(new SqlParameter("@Id", SqlDbType.Int));
        updateCM.Parameters.Add(new SqlParameter("@Name", SqlDbType.NVarChar));
        updateCM.Parameters.Add(new SqlParameter("@Price", SqlDbType.Money));
        updateCM.Parameters.Add(new SqlParameter("@Quantity", SqlDbType.SmallInt));

        updateCM.Parameters["@Id"].SourceVersion = DataRowVersion.Original;
        updateCM.Parameters["@Id"].SourceColumn = "Id";

        updateCM.Parameters["@Name"].SourceVersion = DataRowVersion.Current;
        updateCM.Parameters["@Name"].SourceColumn = "Name";

        updateCM.Parameters["@Price"].SourceVersion = DataRowVersion.Current;
        updateCM.Parameters["@Price"].SourceColumn = "Price";

        updateCM.Parameters["@Quantity"].SourceVersion = DataRowVersion.Current;
        updateCM.Parameters["@Quantity"].SourceColumn = "Quantity";


        adapter.UpdateCommand = updateCM;
        try
        {
            adapter.Update(dataset);

        }
        catch (SqlException ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void DataGridView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var selectedItem = (sender as DataGrid).SelectedItem;

        if (selectedItem is not null)
        {
            var rowView = selectedItem as DataRowView;
            MessageBox.Show($"{rowView["Id"]} {rowView["Name"]} {rowView["Price"]} {rowView["Quantity"]}");
        }
    }

    private async void ComboBox_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        cBox_Categories.Items.Clear();
        conn = new SqlConnection(conStr);
        try
        {
            await conn?.OpenAsync();
            string selCommand = "SELECT [Name] FROM Categories";
            using SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText += selCommand;
            reader = await cmd.ExecuteReaderAsync();



            while (await reader.ReadAsync())
            {
                cBox_Categories.Items.Add(reader[0]);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        finally
        {
            await conn.CloseAsync();
            await reader?.CloseAsync();
        }
    }

    private async void cBox_Categories_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            await conn?.OpenAsync();
            string selcom = @"SELECT Products.Name, Products.Price, Products.Quantity
                              FROM Products
                              JOIN Categories ON CategoryId = Categories.id
                              WHERE Categories.Name = @p1";

            using SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = "WAITFOR DELAY '00:00:05';";
            cmd.CommandText += selcom;
            cmd.Parameters.AddWithValue("@p1", cBox_Categories.SelectedItem.ToString());
            reader = await cmd.ExecuteReaderAsync();

            table = new DataTable();

            int line = 0;
            do
            {
                while (await reader.ReadAsync())
                {
                    if (line == 0)
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        line++;
                    }
                    DataRow row = table.NewRow();

                    for (int i = 0; i < reader.FieldCount; i++)
                        row[i] = await reader.GetFieldValueAsync<object>(i);

                    table.Rows.Add(row);
                }
            } while (reader.NextResult());
            DataGridView.ItemsSource = null;
            DataGridView.ItemsSource = table.AsDataView();

        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error");
            throw;
        }
        finally
        {
            await conn?.CloseAsync();
            await reader.CloseAsync();
        }
    }

    private void Button_Click_2(object sender, RoutedEventArgs e) {}
}
