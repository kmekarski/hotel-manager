using HotelManager.Data;
using HotelManager.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HotelManager
{
    public partial class ClientsView : UserControl
    {
        private HotelContext _context;
        private Client _selectedClient;

        public ClientsView()
        {
            InitializeComponent();
            _context = new HotelContext();
            InitializeAsync();
        }
        private async void InitializeAsync()
        {
            await LoadClients();
        }
        private async Task LoadClients()
        {
            try
            {
                var clients = await _context.Clients.Include(r => r.Reservations).ToListAsync();

                // Setting data in the table
                Dispatcher.Invoke(() =>
                {
                    ClientsGrid.ItemsSource = clients;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"An error occurred while loading clients: {ex.Message}");
                });
            }
        }

        private void ClientsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Check if an item is selected in the DataGrid
            if (ClientsGrid.SelectedItem != null)
            {
                // Cast to dynamic if you're unsure of the row's type
                var selectedClient = ClientsGrid.SelectedItem as dynamic;

                // Ensure the selectedClient object is not null
                if (selectedClient != null)
                {
                    // Safely parse ClientId (if it's an integer)
                    int clientId;
                    if (int.TryParse(selectedClient.ClientId.ToString(), out clientId))
                    {
                        // If parsing succeeded, we can proceed
                        var clientName = selectedClient.Name;
                        var clientEmail = selectedClient.Email;
                        var clientPhoneNumber = selectedClient.PhoneNumber;

                        // Use clientId to find the full Client object in the database
                        var client = _context.Clients.FirstOrDefault(c => c.ClientId == clientId);

                        if (client != null)
                        {
                            // Pass the full client object to the method displaying reservations
                            ShowClientReservations(client);
                        }
                        else
                        {
                            MessageBox.Show("Client not found in the database.");
                        }
                    }
                    else
                    {
                        // If parsing fails
                        MessageBox.Show("Error: Invalid client ID.");
                    }
                }
            }
        }

        private void ShowClientReservations(Client client)
        {
            ReservationsListSection.Visibility = Visibility.Visible;
            ClientsListSection.Visibility = Visibility.Collapsed;
            var reservations = _context.Reservations
                                       .Where(r => r.Client.ClientId == client.ClientId)
                                       .Select(r => new
                                       {
                                           r.ReservationId,
                                           Room = r.Room.Number,
                                           r.StartDate,
                                           r.EndDate
                                       })
                                       .ToList();

            ReservationsForClientGrid.ItemsSource = reservations;

        }

        private void LoadClientReservations()
        {
            // Load reservations for the selected client
            var reservations = _context.Reservations
                                       .Where(r => r.Client.ClientId == _selectedClient.ClientId)
                                       .Select(r => new
                                       {
                                           r.ReservationId,
                                           Room = r.Room.Number,
                                           r.StartDate,
                                           r.EndDate
                                       })
                                       .ToList();

            ReservationsForClientGrid.ItemsSource = reservations;
        }

        // Return to the client list
        private void BackToClientsList(object sender, RoutedEventArgs e)
        {
            ClientsListSection.Visibility = Visibility.Visible;
            ReservationsListSection.Visibility = Visibility.Collapsed;
        }
        private void ShowAddClientForm_Click(object sender, RoutedEventArgs e)
        {
            AddClientSection.Visibility = Visibility.Visible;
            ClientsListSection.Visibility = Visibility.Collapsed;
        }

        private void CancelAddClient_Click(object sender, RoutedEventArgs e)
        {
            AddClientSection.Visibility = Visibility.Collapsed;
            ClientsListSection.Visibility = Visibility.Visible;
        }

        private bool IsValidEmail(string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            return Regex.IsMatch(phoneNumber, @"^\d{9}$"); // Telefon: tylko cyfry, długość 9 znaków
        }

        private void AddClient(object sender, RoutedEventArgs e)
        {
            string name = NewClientNameInput.Text;
            string email = NewClientEmailInput.Text;
            string phoneNumber = NewClientPhoneNumberInput.Text;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phoneNumber))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Invalid email format.");
                return;
            }

            if (!IsValidPhoneNumber(phoneNumber))
            {
                MessageBox.Show("Phone number should contain only digits (9-15 characters).");
                return;
            }

            var existingClient = _context.Clients.FirstOrDefault(c => c.Email == email);
            if (existingClient != null)
            {
                MessageBox.Show("A client with the provided email already exists.");
                return;
            }

            var newClient = new Client
            {
                Name = name,
                Email = email,
                PhoneNumber = phoneNumber
            };

            _context.Clients.Add(newClient);
            _context.SaveChanges();

            MessageBox.Show("Client successfully added.");

            AddClientSection.Visibility = Visibility.Collapsed;
            ClientsListSection.Visibility = Visibility.Visible;
            LoadClients();
        }
        private void ClientsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If an item is selected in the DataGrid
            if (ClientsGrid.SelectedItem != null)
            {
                // Select the selected row as an anonymous object
                var selectedRow = ClientsGrid.SelectedItem as dynamic;

                // If the object contains the required data, map it to a full Reservation instance
                if (selectedRow != null)
                {
                    int clientId = selectedRow.ClientId;
                    string clientName = selectedRow.Name;
                    string clientEmail = selectedRow.Email;
                    string clientPhoneNumber = selectedRow.PhoneNumber;

                    // Convert to a Reservation instance
                    _selectedClient = new Client
                    {
                        ClientId = clientId,
                        Name = clientName,
                        Email = clientEmail,
                        PhoneNumber = clientPhoneNumber
                    };

                    // Populate the form with data from the selected reservation
                    if (_selectedClient != null)
                    {
                        EditClientNameInput.Text = _selectedClient.Name;
                        EditClientEmailInput.Text = _selectedClient.Email;
                        EditClientPhoneNumberInput.Text = _selectedClient.PhoneNumber;

                        EditClientButton.IsEnabled = true;
                        DeleteClientButton.IsEnabled = true;

                    }
                }
            }
            else
            {
                EditClientButton.IsEnabled = false;
                DeleteClientButton.IsEnabled = false;
            }
        }

        // Edit button handler
        private void EditClient_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient != null)
            {
                // Hide the reservation list section and show the edit form
                ClientsListSection.Visibility = Visibility.Collapsed;
                EditClientSection.Visibility = Visibility.Visible;
            }
        }

        // Cancel edit button handler
        private void CancelEditClient_Click(object sender, RoutedEventArgs e)
        {
            // Return to the reservation list
            ClientsListSection.Visibility = Visibility.Visible;
            EditClientSection.Visibility = Visibility.Collapsed;
        }

        // Save changes handler
        private void SaveClientChanges_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient != null)
            {
                var clientName = EditClientNameInput.Text;
                var clientEmail = EditClientEmailInput.Text;
                var clientPhoneNumber = EditClientPhoneNumberInput.Text;

                if (string.IsNullOrWhiteSpace(clientName) || string.IsNullOrWhiteSpace(clientEmail) || string.IsNullOrWhiteSpace(clientPhoneNumber))
                {
                    MessageBox.Show("Please fill in all fields.");
                    return;
                }

                if (!IsValidEmail(clientEmail))
                {
                    MessageBox.Show("Invalid email format.");
                    return;
                }

                if (!IsValidPhoneNumber(clientPhoneNumber))
                {
                    MessageBox.Show("Phone number should contain only digits (9-15 characters).");
                    return;
                }

                try
                {
                    var clientFromDb = _context.Clients.FirstOrDefault(c => c.ClientId == _selectedClient.ClientId);

                    if (clientFromDb != null)
                    {
                        clientFromDb.Name = clientName;
                        clientFromDb.Email = clientEmail;
                        clientFromDb.PhoneNumber = clientPhoneNumber;

                        _context.SaveChanges();

                        MessageBox.Show("Client data has been updated.");
                    }
                    else
                    {
                        MessageBox.Show("Client not found in the database.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occurred while saving changes: " + ex.Message);
                }

                ClientsListSection.Visibility = Visibility.Visible;
                EditClientSection.Visibility = Visibility.Collapsed;
                LoadClients();
            }
        }

        // Delete button handler
        private void DeleteClient_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClient != null)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete client {_selectedClient.Name}?",
                    "Delete Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Find the client in the context based on the primary key
                        var clientToDelete = _context.Clients.FirstOrDefault(c => c.ClientId == _selectedClient.ClientId);

                        if (clientToDelete != null)
                        {
                            // Remove the client from the context
                            _context.Clients.Remove(clientToDelete);

                            // Save changes to the database
                            _context.SaveChanges();

                            MessageBox.Show("Client has been deleted.");

                            // Return to the client list
                            ClientsListSection.Visibility = Visibility.Visible;
                            EditClientSection.Visibility = Visibility.Collapsed;

                            // Refresh the client list
                            LoadClients();
                        }
                        else
                        {
                            MessageBox.Show("Client to delete not found.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while deleting the client: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("No client selected for deletion.");
            }
        }

        private void CancelAddClient(object sender, RoutedEventArgs e)
        {
            AddClientSection.Visibility = Visibility.Collapsed;
            ClientsListSection.Visibility = Visibility.Visible;
            EditClientButton.Visibility = Visibility.Visible;
            DeleteClientButton.Visibility = Visibility.Visible;
        }
    }
}
