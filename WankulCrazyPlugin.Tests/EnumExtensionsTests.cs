using System;
using Xunit;

namespace WankulCrazyPlugin.Tests
{
    public class EnumExtensionsTests
    {
        [Fact]
        public void TryToInt32_SupportsMultipleTypes()
        {
            Assert.True(EnumExtensions.TryToInt32(42, out int res1));
            Assert.Equal(42, res1);

            Assert.True(EnumExtensions.TryToInt32(DayOfWeek.Wednesday, out int res2));
            Assert.Equal((int)DayOfWeek.Wednesday, res2);

            Assert.False(EnumExtensions.TryToInt32("notAnInt", out int res3));
            Assert.Equal(0, res3);

            Assert.False(EnumExtensions.TryToInt32(null, out int res4));
            Assert.Equal(0, res4);
        }

        [Fact]
        public void Patch_Enum_IsDefined_DoesNotThrowOnStringOrInvalidType()
        {
            bool result = false;

            // Test string value for custom enum
            bool handled = Patch_Enum_IsDefined.Prefix(typeof(global::EMonsterType), "WankulMonster001", ref result);
            Assert.False(handled); // Handled by prefix
            Assert.True(result);

            // Test string value for native enum name (passes through)
            bool handledNativeString = Patch_Enum_IsDefined.Prefix(typeof(global::EMonsterType), "EarlyPlayer", ref result);
            Assert.True(handledNativeString); // Laisse passer a l'original

            // Test integer value for custom enum
            bool handledCustomInt = Patch_Enum_IsDefined.Prefix(typeof(global::EMonsterType), 50000, ref result);
            Assert.False(handledCustomInt);
            Assert.True(result);

            // Test unhandled type/value
            bool handledNull = Patch_Enum_IsDefined.Prefix(typeof(global::EMonsterType), null, ref result);
            Assert.True(handledNull);
        }

        [Fact]
        public void Patch_Enum_GetName_ReturnsCustomName()
        {
            string? result = null;

            bool handled = Patch_Enum_GetName.Prefix(typeof(global::EItemType), 125, ref result);
            Assert.False(handled);
            Assert.Equal("BoosterStellar", result);

            // Native or unmapped int passes through
            bool handledNative = Patch_Enum_GetName.Prefix(typeof(global::EItemType), 0, ref result);
            Assert.True(handledNative);
        }

        [Fact]
        public void Patch_Enum_Parse_ParsesCustomValuesCaseInsensitively()
        {
            object? result = null;

            bool handled = Patch_Enum_Parse.Prefix(typeof(global::EItemType), "boosterstellar", true, ref result);
            Assert.False(handled);
            Assert.NotNull(result);
            Assert.Equal((global::EItemType)125, (global::EItemType)result!);

            // Native values pass through to base Enum.Parse
            bool handledNative = Patch_Enum_Parse.Prefix(typeof(global::EItemType), "None", false, ref result);
            Assert.True(handledNative);
        }
    }
}
