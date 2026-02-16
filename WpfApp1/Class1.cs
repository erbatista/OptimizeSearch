using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class MyViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<MyItem> _fullCollection;
        public ObservableCollection<MyItem> FullCollection
        {
            get => _fullCollection;
            set
            {
                _fullCollection = value;
                OnPropertyChanged(nameof(FullCollection));
                OnPropertyChanged(nameof(FirstThreeItems)); // Update derived property
            }
        }

        // Property exposing only the first three items
        public IEnumerable<MyItem> FirstThreeItems => FullCollection?.Take(3);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

/*
 using Microsoft.VisualStudio.TestTools.UnitTesting;
   using System.Linq;
   
   namespace ApolloERP.Ics.Tests.ChannelLogic
   {
       [TestClass]
       public class ChannelSelectionDataTests
       {
           private ChannelSelectionData _repository;
   
           [TestInitialize]
           public void Setup()
           {
               // Initialize fresh before every test
               _repository = new ChannelSelectionData();
           }
   
           [TestMethod]
           public void Grid_24GHz_Row1_StartsAt1_EndsAt9()
           {
               // ARRANGE
               var channels = _repository.GetAvailableChannels().ToList();
   
               // ACT - Get Channel 1 and Channel 9
               var ch1 = channels.First(c => c.Channel.Band == BandType.Band2_4GHz && c.Channel.Number == 1);
               var ch9 = channels.First(c => c.Channel.Band == BandType.Band2_4GHz && c.Channel.Number == 9);
   
               // ASSERT
               Assert.IsTrue(ch1.IsLeft, "Channel 1 should be the START of the row (Left).");
               Assert.IsFalse(ch1.IsRight, "Channel 1 should NOT be Right.");
   
               Assert.IsTrue(ch9.IsRight, "Channel 9 should be the END of the row (Right).");
               Assert.IsFalse(ch9.IsLeft, "Channel 9 should NOT be Left.");
           }
   
           [TestMethod]
           public void Grid_Resets_ForNewBand()
           {
               // This test ensures that the grid count resets when 5GHz starts.
               // Even if 2.4GHz ended in the middle of a row, 5GHz must start a NEW row (IsLeft = true).
   
               // ARRANGE
               var channels = _repository.GetAvailableChannels().ToList();
   
               // 2.4GHz ends at Channel 14. 
               // 1-9 is Row 1. 10-14 is Row 2 (Partial, so 5 items).
               var ch14 = channels.First(c => c.Channel.Number == 14);
   
               // 5GHz (Legacy) starts at 34.
               var ch34 = channels.First(c => c.Channel.Band == BandType.Band5GHz && c.Channel.Number == 34);
   
               // ASSERT
               Assert.IsFalse(ch14.IsRight, "Channel 14 is the 5th item in Row 2, so it is NOT Right edge.");
   
               // CRITICAL CHECK:
               Assert.IsTrue(ch34.IsLeft, "Channel 34 (First 5GHz) must start a NEW row (IsLeft).");
           }
   
           [TestMethod]
           public void Grid_5GHzLegacy_CheckAlignment()
           {
               // 5GHz Legacy Logic Sequence: 
               // Index 1: 34 (Left)
               // Index 2: 36
               // ...
               // Index 9: 52 (Right) -- This is the 9th item in the generated list.
   
               // ARRANGE
               var channels = _repository.GetAvailableChannels().ToList();
               var ch52 = channels.First(c => c.Channel.Band == BandType.Band5GHz && c.Channel.Number == 52);
   
               // ASSERT
               Assert.IsTrue(ch52.IsRight, "Channel 52 should be the 9th item in the 5GHz list, so it is Right edge.");
           }
   
           [TestMethod]
           public void Grid_6GHz_BasicCheck()
           {
               // 6GHz Logic: 1, 5, 9...
               // Row 1: 1, 5, 9, 13, 17, 21, 25, 29, 33
               // So Ch 1 is Left, Ch 33 is Right.
   
               // ARRANGE
               var channels = _repository.GetAvailableChannels().ToList();
               var ch1_6g = channels.First(c => c.Channel.Band == BandType.Band6GHz && c.Channel.Number == 1);
               var ch33_6g = channels.First(c => c.Channel.Band == BandType.Band6GHz && c.Channel.Number == 33);
   
               // ASSERT
               Assert.IsTrue(ch1_6g.IsLeft, "6GHz Channel 1 must be Left.");
               Assert.IsTrue(ch33_6g.IsRight, "6GHz Channel 33 (9th item) must be Right.");
           }
       }
   }
 */
