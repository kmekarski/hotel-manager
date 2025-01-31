using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HotelManager.Data;
using HotelManager.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelManager
{
    public partial class ReservationsView : UserControl
    {
        private HotelContext _context;
        private Reservation? _selectedReservation;

        public ReservationsView()
        {
            InitializeComponent();
            _context = new HotelContext();
            LoadReservation();
        }

        private async void LoadReservation()
        {
            await LoadReservationsAsync();
        }
        private async Task LoadReservationsAsync()
        {
            var reservations = await _context.Reservations
                .Select(r => new
                {
                    r.ReservationId,
                    Client = r.Client.Name,
                    r.Client.Email,
                    r.Client.PhoneNumber,
                    Room = r.Room.Number,
                    r.StartDate,
                    r.EndDate
                })
                .ToListAsync();

            ReservationsGrid.ItemsSource = reservations;
        }

        private void ShowAddReservationForm(object sender, RoutedEventArgs e)
        {
            ReservationsListSection.Visibility = Visibility.Collapsed;
            AddReservationSection.Visibility = Visibility.Visible;
            EditReservationButton.Visibility = Visibility.Collapsed;
            DeleteReservationButton.Visibility = Visibility.Collapsed;
        }

        private void ReservationsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if (ReservationsGrid.SelectedItem != null)
            {
                
                var selectedRow = ReservationsGrid.SelectedItem as dynamic;

         
                if (selectedRow != null)
                {
                    int reservationId = selectedRow.ReservationId;
                    string clientName = selectedRow.Client;
                    string clientEmail = selectedRow.Email;
                    string clientPhoneNumber = selectedRow.PhoneNumber;
                    int roomNumber = selectedRow.Room;
                    DateTime startDate = selectedRow.StartDate;
                    DateTime endDate = selectedRow.EndDate;


                    _selectedReservation = new Reservation
                    {
                        ReservationId = reservationId,
                        Client = _context.Clients.FirstOrDefault(c => c.Name == clientName), 
                        Room = _context.Rooms.FirstOrDefault(r => r.Number == roomNumber), 
                        StartDate = startDate,
                        EndDate = endDate
                    };

                    
                    if (_selectedReservation != null)
                    {
                        EditClientNameInput.Text = _selectedReservation.Client.Name;
                        EditClientEmailInput.Text = _selectedReservation.Client.Email;
                        EditClientPhoneNumberInput.Text = _selectedReservation.Client.PhoneNumber;
                        EditRoomNumberInput.Text = _selectedReservation.Room.Number.ToString();
                        EditStartDateInput.SelectedDate = _selectedReservation.StartDate;
                        EditEndDateInput.SelectedDate = _selectedReservation.EndDate;

                        
                        EditReservationButton.IsEnabled = true;
                        DeleteReservationButton.IsEnabled = true;
                    }
                }
            }
            else
            {
                EditReservationButton.IsEnabled = false;
                DeleteReservationButton.IsEnabled = false;
            }
        }

        // Edit button handler
        private void EditReservation_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedReservation != null)
            {
                ReservationsListSection.Visibility = Visibility.Collapsed;
                EditReservationSection.Visibility = Visibility.Visible;
            }
        }

        // Cancel editing handler
        private void CancelEditReservation(object sender, RoutedEventArgs e)
        {
            // Return to the reservation list
            ReservationsListSection.Visibility = Visibility.Visible;
            EditReservationSection.Visibility = Visibility.Collapsed;
        }

        // Save changes handler
        private void SaveReservationChanges_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedReservation != null)
            {
                var clientName = EditClientNameInput.Text;
                var clientEmail = EditClientEmailInput.Text;
                var clientPhoneNumber = EditClientPhoneNumberInput.Text;
                var roomNumber = EditRoomNumberInput.Text;
                var startDate = EditStartDateInput.SelectedDate.Value;
                var endDate = EditEndDateInput.SelectedDate.Value;

                // Validate phone number
                if (!System.Text.RegularExpressions.Regex.IsMatch(clientPhoneNumber, @"^\d{9,15}$"))
                {
                    MessageBox.Show("Phone number should contain between 9 and 15 digits.");
                    return;
                }

                // Validate email
                if (!System.Text.RegularExpressions.Regex.IsMatch(clientEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    MessageBox.Show("Please enter a valid email address.");
                    return;
                }

                // Check if data is valid
                if (string.IsNullOrWhiteSpace(clientName) || string.IsNullOrWhiteSpace(clientEmail) ||
                    string.IsNullOrWhiteSpace(clientPhoneNumber) || string.IsNullOrWhiteSpace(roomNumber) ||
                    startDate == default || endDate == default)
                {
                    MessageBox.Show("Please fill in all fields.");
                    return;
                }

                // Find client by email
                var client = _context.Clients.FirstOrDefault(c => c.Email == clientEmail);
                if (client == null)
                {
                    client = new Client
                    {
                        Name = clientName,
                        Email = clientEmail,
                        PhoneNumber = clientPhoneNumber
                    };
                    _context.Clients.Add(client);
                    _context.SaveChanges();
                }
                else
                {
                    client.Name = clientName;
                    client.PhoneNumber = clientPhoneNumber;
                    _context.SaveChanges();
                }

                var room = _context.Rooms.FirstOrDefault(r => r.Number == int.Parse(roomNumber));
                if (room == null)
                {
                    MessageBox.Show("A room with the given number does not exist.");
                    return;
                }

                bool isRoomAvailable = !_context.Reservations
                    .Any(r => r.Room.Number == int.Parse(roomNumber) &&
                              r.ReservationId != _selectedReservation.ReservationId &&
                              r.StartDate < endDate &&
                              r.EndDate > startDate);

                if (!isRoomAvailable)
                {
                    MessageBox.Show("The room is already reserved for the given period.");
                    return;
                }

                _selectedReservation.Client = client;
                _selectedReservation.Room = room;
                _selectedReservation.StartDate = startDate;
                _selectedReservation.EndDate = endDate;

                _context.SaveChanges();

                MessageBox.Show("Reservation has been updated.");

                ReservationsListSection.Visibility = Visibility.Visible;
                EditReservationSection.Visibility = Visibility.Collapsed;

                LoadReservationsAsync();
            }
        }

        // Delete button handler
        private void DeleteReservation_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedReservation != null)
            {
                var result = MessageBox.Show("Are you sure you want to delete this reservation?", "Confirmation", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    _context.Reservations.Remove(_selectedReservation);
                    _context.SaveChanges();

                    // Success message
                    MessageBox.Show("Reservation has been deleted.");

                    // Return to the reservation list
                    ReservationsListSection.Visibility = Visibility.Visible;
                    EditReservationSection.Visibility = Visibility.Collapsed;

                    // Reload reservations
                    LoadReservationsAsync();
                }
            }
        }

        private void CancelAddReservation(object sender, RoutedEventArgs e)
        {
            AddReservationSection.Visibility = Visibility.Collapsed;
            ReservationsListSection.Visibility = Visibility.Visible;
            EditReservationButton.Visibility = Visibility.Visible;
            DeleteReservationButton.Visibility = Visibility.Visible;
        }

        private void AddReservation_Click(object sender, RoutedEventArgs e)
        {
            var clientName = ClientNameInput.Text;
            var clientEmail = ClientEmailInput.Text;
            var clientPhoneNumber = ClientPhoneNumberInput.Text;
            var roomNumber = RoomNumberInput.Text;
            var startDate = StartDateInput.SelectedDate.Value;
            var endDate = EndDateInput.SelectedDate.Value;

            // Validate phone number
            if (!System.Text.RegularExpressions.Regex.IsMatch(clientPhoneNumber, @"^\d{9,15}$"))
            {
                MessageBox.Show("Phone number should contain between 9 and 15 digits.");
                return;
            }

            // Validate email
            if (!System.Text.RegularExpressions.Regex.IsMatch(clientEmail, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Please enter a valid email address.");
                return;
            }

            if (string.IsNullOrWhiteSpace(clientName) || string.IsNullOrWhiteSpace(clientEmail) ||
                string.IsNullOrWhiteSpace(clientPhoneNumber) || string.IsNullOrWhiteSpace(roomNumber) ||
                startDate == default || endDate == default)
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            bool isRoomAvailable = !_context.Reservations
                .Any(r => r.Room.Number == int.Parse(roomNumber) &&
                          r.StartDate < endDate &&
                          r.EndDate > startDate);

            if (!isRoomAvailable)
            {
                MessageBox.Show("The room is already reserved for the given period.");
                return;
            }

            var client = _context.Clients.FirstOrDefault(c => c.Email == clientEmail);

            if (client == null)
            {
                client = new Client
                {
                    Name = clientName,
                    Email = clientEmail,
                    PhoneNumber = clientPhoneNumber
                };
                _context.Clients.Add(client);
                _context.SaveChanges();
            }

            var room = _context.Rooms.FirstOrDefault(r => r.Number == int.Parse(roomNumber));

            if (room == null)
            {
                MessageBox.Show("A room with the given number does not exist.");
                return;
            }

            var newReservation = new Reservation
            {
                Client = client,
                Room = room,
                StartDate = startDate,
                EndDate = endDate
            };

            _context.Reservations.Add(newReservation);
            _context.SaveChanges();

            MessageBox.Show("Reservation has been added.");

            ClientNameInput.Clear();
            ClientEmailInput.Clear();
            ClientPhoneNumberInput.Clear();
            RoomNumberInput.Clear();
            StartDateInput.SelectedDate = null;
            EndDateInput.SelectedDate = null;

            LoadReservationsAsync();

            ReservationsListSection.Visibility = Visibility.Visible;
            AddReservationSection.Visibility = Visibility.Collapsed;
        }

    }
}
