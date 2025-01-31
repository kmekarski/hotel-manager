using HotelManager.Data;
using HotelManager.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace HotelManager
{
    public partial class RoomsView : UserControl
    {
        private HotelContext _context;
        private Room? _selectedRoom;

        public RoomsView()
        {
            InitializeComponent();
            _context = new HotelContext();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await LoadRooms();
        }

        private async Task LoadRooms()
        {
            try
            {
                var rooms = await _context.Rooms.Include(r => r.Reservations).ToListAsync();

                // Setting data in the table
                Dispatcher.Invoke(() =>
                {
                    RoomsGrid.ItemsSource = rooms;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"An error occurred while loading rooms: {ex.Message}");
                });
            }
        }

        // Handling double-click on a room
        private void RoomsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Check if a room was selected
            // Check if an item in the DataGrid was selected
            if (RoomsGrid.SelectedItem != null)
            {
                // Casting to a dynamic object if unsure of the row type
                var selectedRoom = RoomsGrid.SelectedItem as dynamic;

                // Check if the selectedRoom object is not null
                if (selectedRoom != null)
                {
                    int roomId;
                    if (int.TryParse(selectedRoom.RoomId.ToString(), out roomId))
                    {
                        // If parsing succeeds, we can proceed
                        var roomNumber = selectedRoom.Number;
                        var roomType = selectedRoom.Type;
                        var roomIsAvailable = selectedRoom.IsAvailable;

                        // Use roomId to find the full Room object in the database
                        var room = _context.Rooms.FirstOrDefault(r => r.RoomId == roomId);

                        if (room != null)
                        {
                            // Pass the full Room object to the method displaying reservations
                            ShowRoomReservations(room);
                        }
                        else
                        {
                            MessageBox.Show("Room not found in the database.");
                        }
                    }
                    else
                    {
                        // If parsing fails
                        MessageBox.Show("Error: Invalid room identifier.");
                    }
                }
            }
        }
        private void ShowRoomReservations(Room room)
        {
            ReservationsListSection.Visibility = Visibility.Visible;
            RoomsListSection.Visibility = Visibility.Collapsed;
            var reservations = _context.Reservations
                                       .Where(r => r.Room.RoomId == room.RoomId)
                                       .Select(r => new
                                       {
                                           r.ReservationId,
                                           Client = r.Client.Name,
                                           r.StartDate,
                                           r.EndDate
                                       })
                                       .ToList();

            ReservationsForRoomsGrid.ItemsSource = reservations;
        }
        private void ShowRoomDetails(Room room)
        {
            // Toggle section visibility
            RoomsListSection.Visibility = Visibility.Collapsed;
        }

        // Returning to the room list
        private void BackToRoomsList(object sender, RoutedEventArgs e)
        {
            RoomsListSection.Visibility = Visibility.Visible;
            ReservationsListSection.Visibility = Visibility.Collapsed;
        }

        // Handling the button to add a new room
        private void ShowAddRoomForm_Click(object sender, RoutedEventArgs e)
        {
            AddRoomSection.Visibility = Visibility.Visible;
            RoomsListSection.Visibility = Visibility.Collapsed;
        }

        private void CancelAddRoom_Click(object sender, RoutedEventArgs e)
        {
            AddRoomSection.Visibility = Visibility.Collapsed;
            RoomsListSection.Visibility = Visibility.Visible;
        }
        private void FilterRooms_Click(object sender, RoutedEventArgs e)
        {
            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;

            if (startDate == null || endDate == null || startDate > endDate)
            {
                MessageBox.Show("Please select a valid date range.");
                return;
            }

            var filteredRooms = _context.Rooms.Where(room =>
                room.IsAvailable &&
                !room.Reservations.Any(reservation =>
                    (reservation.StartDate < endDate && reservation.EndDate > startDate)
                )
            ).ToList();

            // Display the filtered rooms
            RoomsGrid.ItemsSource = filteredRooms;
        }

        // Adding a room
        private void AddRoom(object sender, RoutedEventArgs e)
        {
            string roomNumber = NewRoomNumberInput.Text;
            string roomType = NewRoomTypeInput.Text;
            bool isAvailable = NewRoomIsAvailableInput.IsChecked.GetValueOrDefault();

            if (string.IsNullOrWhiteSpace(roomNumber) || string.IsNullOrWhiteSpace(roomType))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            var existingRoom = _context.Rooms.FirstOrDefault(r => r.Number.ToString() == roomNumber);
            if (existingRoom != null)
            {
                MessageBox.Show("A room with this number already exists.");
                return;
            }

            var newRoom = new Room
            {
                Number = int.Parse(roomNumber),
                Type = roomType,
                IsAvailable = isAvailable
            };

            _context.Rooms.Add(newRoom);
            _context.SaveChanges();

            MessageBox.Show("Room has been successfully added.");

            // Hide the form and reload the view
            AddRoomSection.Visibility = Visibility.Collapsed;
            RoomsListSection.Visibility = Visibility.Visible;
            InitializeAsync(); // Reload the room list
        }

        private void RoomsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RoomsGrid.SelectedItem != null)
            {
                var selectedRoom = RoomsGrid.SelectedItem as Room;

                if (selectedRoom != null)
                {
                    _selectedRoom = selectedRoom;

                    // Populate the form with data from the selected room
                    EditRoomNumberInput.Text = _selectedRoom.Number.ToString();
                    EditRoomTypeInput.Text = _selectedRoom.Type;
                    EditRoomIsAvailableInput.IsChecked = _selectedRoom.IsAvailable;

                    EditRoomButton.IsEnabled = true;
                    DeleteRoomButton.IsEnabled = true;
                }
            }
            else
            {
                EditRoomButton.IsEnabled = false;
                DeleteRoomButton.IsEnabled = false;
            }
        }

        // Editing a room
        private void EditRoom_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRoom != null)
            {
                RoomsListSection.Visibility = Visibility.Collapsed;
                EditRoomSection.Visibility = Visibility.Visible;
            }
        }

        // Canceling room edit
        private void CancelEditRoom_Click(object sender, RoutedEventArgs e)
        {
            RoomsListSection.Visibility = Visibility.Visible;
            EditRoomSection.Visibility = Visibility.Collapsed;
        }

        // Saving room changes
        private void SaveRoomChanges_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRoom != null)
            {
                var roomNumber = EditRoomNumberInput.Text;
                var roomType = EditRoomTypeInput.Text;
                var isAvailable = EditRoomIsAvailableInput.IsChecked.GetValueOrDefault();

                if (string.IsNullOrWhiteSpace(roomNumber) || string.IsNullOrWhiteSpace(roomType))
                {
                    MessageBox.Show("Please fill in all fields.");
                    return;
                }

                try
                {
                    var roomFromDb = _context.Rooms.FirstOrDefault(r => r.RoomId == _selectedRoom.RoomId);

                    if (roomFromDb != null)
                    {
                        roomFromDb.Number = int.Parse(roomNumber);
                        roomFromDb.Type = roomType;
                        roomFromDb.IsAvailable = isAvailable;

                        _context.SaveChanges();

                        MessageBox.Show("Room has been updated.");
                    }
                    else
                    {
                        MessageBox.Show("Room not found in the database.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occurred while saving changes: " + ex.Message);
                }

                RoomsListSection.Visibility = Visibility.Visible;
                EditRoomSection.Visibility = Visibility.Collapsed;
                InitializeAsync();
            }
        }

        // Deleting a room
        private void DeleteRoom_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRoom != null)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete room {_selectedRoom.Number}?",
                    "Delete Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var roomToDelete = _context.Rooms.FirstOrDefault(r => r.RoomId == _selectedRoom.RoomId);

                        if (roomToDelete != null)
                        {
                            _context.Rooms.Remove(roomToDelete);
                            _context.SaveChanges();

                            MessageBox.Show("Room has been deleted.");

                            RoomsListSection.Visibility = Visibility.Visible;
                            EditRoomSection.Visibility = Visibility.Collapsed;

                            InitializeAsync();
                        }
                        else
                        {
                            MessageBox.Show("Room to delete not found.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while deleting the room: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("No room selected for deletion.");
            }
        }
    }
}
