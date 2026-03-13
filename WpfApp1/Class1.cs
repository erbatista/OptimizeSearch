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
            _repository = new ChannelSelectionData();
        }

        [TestMethod]
        public void Grid_Row1_Pure24GHz_StartsAt1_EndsAt9()
        {
            // ARRANGE
            var channels = _repository.GetAvailableChannels().ToList();

            // ACT
            // Row 1 uses indices 1 through 9.
            var ch1 = channels.First(c => c.Channel.Band == BandType.Band2_4GHz && c.Channel.Number == 1);
            var ch9 = channels.First(c => c.Channel.Band == BandType.Band2_4GHz && c.Channel.Number == 9);

            // ASSERT
            Assert.IsTrue(ch1.IsLeft, "Index 1 (2.4GHz Ch 1) should be Left edge.");
            Assert.IsTrue(ch9.IsRight, "Index 9 (2.4GHz Ch 9) should be Right edge.");
        }

        [TestMethod]
        public void Grid_Row2_MixedBands_ContinuousFlow()
        {
            // ARRANGE
            var channels = _repository.GetAvailableChannels().ToList();

            // Row 2 uses indices 10 through 18.
            // 2.4GHz provides indices 10-14 (Channels 10, 11, 12, 13, 14).
            // 5GHz provides indices 15-18 (Channels 34, 36, 38, 40).

            // ACT
            var ch10 = channels.First(c => c.Channel.Band == BandType.Band2_4GHz && c.Channel.Number == 10);
            var ch34 = channels.First(c => c.Channel.Band == BandType.Band5GHz && c.Channel.Number == 34);
            var ch40 = channels.First(c => c.Channel.Band == BandType.Band5GHz && c.Channel.Number == 40);

            // ASSERT
            Assert.IsTrue(ch10.IsLeft, "Index 10 (2.4GHz Ch 10) should be Left edge of Row 2.");

            // Because of continuous flow, Ch 34 is in the middle of Row 2, NOT a left edge.
            Assert.IsFalse(ch34.IsLeft, "Index 15 (5GHz Ch 34) should NOT be Left edge. It flows continuously.");

            // The right edge of Row 2 is index 18, which falls on 5GHz Channel 40.
            Assert.IsTrue(ch40.IsRight, "Index 18 (5GHz Ch 40) should be Right edge of Row 2.");
        }

        [TestMethod]
        public void Grid_Row3_Pure5GHz_ValidatesBugFix()
        {
            // ARRANGE
            var channels = _repository.GetAvailableChannels().ToList();

            // Row 3 uses indices 19 through 27.
            // 5GHz continues: 42(19), 44(20), 46(21), 48(22), 52(23), 56(24), 60(25), 64(26), 100(27).

            // ACT
            var ch42 = channels.First(c => c.Channel.Band == BandType.Band5GHz && c.Channel.Number == 42);
            var ch52 = channels.First(c => c.Channel.Band == BandType.Band5GHz && c.Channel.Number == 52);
            var ch100 = channels.First(c => c.Channel.Band == BandType.Band5GHz && c.Channel.Number == 100);

            // ASSERT
            Assert.IsTrue(ch42.IsLeft, "Index 19 (5GHz Ch 42) should be Left edge of Row 3.");

            // This explicitly tests the bug you caught with the debugger!
            Assert.IsFalse(ch52.IsRight, "BUG FIX: Index 23 (5GHz Ch 52) is in the middle of Row 3, NOT a Right edge.");
            Assert.IsFalse(ch52.IsLeft, "BUG FIX: Index 23 (5GHz Ch 52) is NOT a Left edge.");

            // The actual right edge of Row 3 is index 27
            Assert.IsTrue(ch100.IsRight, "Index 27 (5GHz Ch 100) should be the Right edge of Row 3.");
        }

        [TestMethod]
        public void Grid_Row5_Mixed5GHzAnd6GHz()
        {
            // ARRANGE
            var channels = _repository.GetAvailableChannels().ToList();

            // Row 5 uses indices 37 through 45.
            // 5GHz provides indices 37-42 (Channels 140, 149, 153, 157, 161, 165).
            // 6GHz starts at index 43, providing 43-45 (Channels 1, 5, 9).

            // ACT
            var ch140 = channels.First(c => c.Channel.Band == BandType.Band5GHz && c.Channel.Number == 140);
            var ch1_6g = channels.First(c => c.Channel.Band == BandType.Band6GHz && c.Channel.Number == 1);
            var ch9_6g = channels.First(c => c.Channel.Band == BandType.Band6GHz && c.Channel.Number == 9);
            var ch13_6g = channels.First(c => c.Channel.Band == BandType.Band6GHz && c.Channel.Number == 13);

            // ASSERT
            Assert.IsTrue(ch140.IsLeft, "Index 37 (5GHz Ch 140) should be Left edge of Row 5.");

            Assert.IsFalse(ch1_6g.IsLeft, "Index 43 (6GHz Ch 1) flows continuously, it is NOT Left edge.");

            Assert.IsTrue(ch9_6g.IsRight, "Index 45 (6GHz Ch 9) should be Right edge of Row 5.");

            Assert.IsTrue(ch13_6g.IsLeft, "Index 46 (6GHz Ch 13) should be Left edge of Row 6.");
        }
    }
}
  */
