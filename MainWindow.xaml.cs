using System;
using System.Windows;
using System.Windows.Controls;

namespace HotelManager
{
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            MainContent.Content = new WelcomeView();
        }

        private void ShowRoomsView(object sender, RoutedEventArgs e)
        {
           MainContent.Content = new RoomsView(); // UserControl for managing rooms

        }

        private void ShowReservationsView(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new ReservationsView(); // UserControl for managing reservations
        }

        private void ShowClientsView(object sender, RoutedEventArgs e)
        {
         
            MainContent.Content = new ClientsView(); // UserControl for managing clients
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
           

            MainContent.Content = new WelcomeView(); // UserControl for welcome screen
        }
       private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

     
    }
}
