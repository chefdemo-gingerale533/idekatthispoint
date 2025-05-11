using System;
using System.Data.SQLite;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Terminal.Gui;

class Program
{
    public static void Main()
    {
        Application.Init();
        var top = Application.Top;

        // Initialize SQLite database and load products
        InitDatabase();
        var products = LoadProducts();

        // UI elements
        var win = new Window("Point of Sale System")
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // Add color to the menu bar
        var menu = new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_File", new MenuItem[]
            {
                new MenuItem("_Quit", "", () => 
                {
                    ClearScreen();
                    Application.RequestStop();
                })
            })
        })
        {
            ColorScheme = new ColorScheme
            {
                Normal = Terminal.Gui.Attribute.Make(Color.BrightCyan, Color.Black),
                Focus = Terminal.Gui.Attribute.Make(Color.BrightYellow, Color.Black)
            }
        };

        var cart = new System.Collections.Generic.List<(string Name, double Price)>();
        var cartListView = new ListView(cart.Select(c => $"{c.Name} - ${c.Price:F2}").ToList())
        {
            X = 2,
            Y = 2,
            Width = Dim.Percent(50),
            Height = Dim.Fill(),
            CanFocus = true,
            ColorScheme = new ColorScheme
            {
                Normal = Terminal.Gui.Attribute.Make(Color.BrightGreen, Color.Black),
                Focus = Terminal.Gui.Attribute.Make(Color.White, Color.Black)
            }
        };

        var productListView = new ListView(products.Select(p => $"{p.Name} - ${p.Price:F2}").ToList())
        {
            X = Pos.Right(cartListView) + 2,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
            ColorScheme = new ColorScheme
            {
                Normal = Terminal.Gui.Attribute.Make(Color.BrightMagenta, Color.Black),
                Focus = Terminal.Gui.Attribute.Make(Color.White, Color.Black)
            }
        };

        var checkoutButton = new Button("Checkout")
        {
            X = Pos.Center(),
            Y = Pos.Bottom(productListView) - 2,
            ColorScheme = new ColorScheme
            {
                Normal = Terminal.Gui.Attribute.Make(Color.BrightRed, Color.Black),
                Focus = Terminal.Gui.Attribute.Make(Color.White, Color.Black)
            }
        };

        checkoutButton.Clicked += async () =>
        {
            if (cart.Count == 0)
            {
                MessageBox.ErrorQuery("Error", "Cart is empty!", "OK");
                return;
            }

            await Checkout(cart);
            cart.Clear();
            cartListView.SetSource(cart.Select(c => $"{c.Name} - ${c.Price:F2}").ToList());
        };

        productListView.SelectedItemChanged += (args) =>
        {
            if (args.Item >= 0 && args.Item < products.Count)
            {
                cart.Add(products[args.Item]);
                cartListView.SetSource(cart.Select(c => $"{c.Name} - ${c.Price:F2}").ToList());
            }
        };

        // Add elements to the main window
        win.Add(cartListView, productListView, checkoutButton);
        top.Add(menu, win);

        // Run the application
        Application.Run();
    }

    static void InitDatabase()
    {
        using var connection = new SQLiteConnection("Data Source=pos.db;Version=3;");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Price REAL NOT NULL
            );

            INSERT OR IGNORE INTO Products (Id, Name, Price) VALUES
                (1, 'Coke', 1.5),
                (2, 'Burger', 5.0),
                (3, 'Fries', 2.5);
        ";
        command.ExecuteNonQuery();
    }

    static System.Collections.Generic.List<(string Name, double Price)> LoadProducts()
    {
        using var connection = new SQLiteConnection("Data Source=pos.db;Version=3;");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Name, Price FROM Products";
        var reader = command.ExecuteReader();

        var products = new System.Collections.Generic.List<(string Name, double Price)>();
        while (reader.Read())
        {
            products.Add((reader.GetString(0), reader.GetDouble(1)));
        }
        return products;
    }

    static async Task Checkout(System.Collections.Generic.List<(string Name, double Price)> cart)
    {
        var total = cart.Sum(item => item.Price);

        var robloxUsername = InputDialog("Enter Roblox Username:");
        if (string.IsNullOrEmpty(robloxUsername)) return;

        var discordUsername = InputDialog("Enter Discord Username:");
        if (string.IsNullOrEmpty(discordUsername)) return;

        var result = MessageBox.Query(
            "Checkout", 
            $"Roblox User: {robloxUsername}\nDiscord User: {discordUsername}\nTotal: ${total:F2}\n\nPress OK to confirm checkout.", 
            "OK", "Cancel");

        if (result == 0)
        {
            // Send licensing data to server
            var success = await SendLicensingDataToServer(robloxUsername, discordUsername, cart, total);

            if (success)
            {
                // Display confirmation in a dialog box
                InfoDialog("Checkout Complete", $"Thank you for your purchase!\nTotal: ${total:F2}");
            }
            else
            {
                MessageBox.ErrorQuery("Error", "Failed to send licensing data to the server.", "OK");
            }
        }
    }

    static async Task<bool> SendLicensingDataToServer(string robloxUsername, string discordUsername, System.Collections.Generic.List<(string Name, double Price)> cart, double total)
    {
        try
        {
            using var httpClient = new HttpClient();
            var licensingData = new
            {
                RobloxUsername = robloxUsername,
                DiscordUsername = discordUsername,
                Items = cart.Select(item => new { item.Name, item.Price }),
                Total = total
            };

            var json = JsonSerializer.Serialize(licensingData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://your-licensing-server.com/api/licenses", content);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending licensing data: {ex.Message}");
            return false;
        }
    }

    static string InputDialog(string prompt)
    {
        var dialog = new Dialog(prompt, 60, 7);
        var input = new TextField("")
        {
            X = 2,
            Y = 2,
            Width = Dim.Fill() - 4
        };

        var okButton = new Button("OK")
        {
            X = Pos.Center(),
            Y = Pos.Bottom(input) + 1
        };

        okButton.Clicked += () => Application.RequestStop();

        dialog.Add(input, okButton);
        Application.Run(dialog);

        return input.Text?.ToString() ?? string.Empty;
    }

    static void InfoDialog(string title, string message)
    {
        var dialog = new Dialog(title, 60, 10);
        var label = new Label(message)
        {
            X = Pos.Center(),
            Y = Pos.Center()
        };

        var okButton = new Button("OK")
        {
            X = Pos.Center(),
            Y = Pos.Bottom(label) + 1
        };

        okButton.Clicked += () => Application.RequestStop();

        dialog.Add(label, okButton);
        Application.Run(dialog);
    }

    static void ClearScreen()
    {
        Console.Clear();
    }
}
