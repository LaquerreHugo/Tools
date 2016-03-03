using System;
using System.Threading.Tasks;
using Microsoft.Data.Entity;


namespace Tools
{
    //Todo: Update Flags summary.
    //Todo: Finish Flags documentation
    /// <summary>
    /// Represent flags.
    /// </summary>
    public static class Flags<TContext> where TContext: DbContext, IFlagEntities, new() {
        #region Get
        #region Sync
        /// <summary>
        /// Tries to get a flag's value as <see cref="String"/> from the flag's name.
        /// </summary>
        /// <param name="flagName">The flag's name.</param>
        /// <returns>Returns a <see cref="String"/> representing the flag's value. Returns <code>null</code> if no flags match the specified name.</returns>
        /// <exception cref="ArgumentException">The flag's name cannot be <code>null</code> or empty.</exception>
        public static String Get(String flagName)
            => GetAsync(flagName).Result;

        /// <summary>
        /// Tries to get a flag's value as the specified type from the flag's name using a specified conversion method.
        /// </summary>
        /// <typeparam name="T">The type of the flag's value.</typeparam>
        /// <param name="flagName">The flag's name</param>
        /// <param name="conversionMethod">The method used to convert the value from <see cref="String"/> to <see cref="T"/>.</param>
        /// <returns>Returns a <see cref="T"/> representing the flag's value. Returns the default, or safe null, for the specified type if no flags match the specified name.</returns>
        public static T Get<T>(String flagName, Func<String, T> conversionMethod)
            => GetAsync(flagName, conversionMethod).Result;

        /// <summary>
        /// Tries to get a flag's value as the specified type from the flag's name using a safe cast.
        /// </summary>
        /// <typeparam name="T">The type of the flag's value.</typeparam>
        /// <param name="flagName">The flag's name.</param>
        /// <returns>Returns a <see cref="T"/> representing the flag's value. Returns the default, or safe null, for the specified type if no flags match the specified name or if conversion does not work.</returns>
        public static T Get<T>(String flagName) where T : class
            => Get(flagName) as T;
        #endregion


        /// <summary>
        /// Tries to get a flag's value asynchronously as <see cref="String"/> from the flag's name.
        /// </summary>
        /// <param name="flagName">The flag's name.</param>
        /// <returns>Returns a <see cref="String"/> representing the flag's value. Returns <code>null</code> if no flags match the specified name.</returns>
        /// <exception cref="ArgumentException">The flag's name cannot be <code>null</code> or empty.</exception>
        public static async Task<String> GetAsync(String flagName) {
            //Exceptions
            if (String.IsNullOrEmpty(flagName))
                throw new ArgumentException("Argument is null or empty", nameof(flagName));

            //Get
            using (var db = new TContext()) {
                var flag = await db.Flags
                    .SingleOrDefaultAsync(f => f.Name.Equals(flagName));
                return flag?.Value;
            }
        }

        /// <summary>
        /// Tries to get a flag's value as the specified type asynchronously from the flag's name using a specified asynchronous conversion method.
        /// </summary>
        /// <typeparam name="T">The type of the flag's value.</typeparam>
        /// <param name="flagName">The flag's name</param>
        /// <param name="asyncConversionMethod">The asynchronous method used to convert the value from <see cref="String"/> to <see cref="T"/>.</param>
        /// <returns>Returns a <see cref="T"/> representing the flag's value. Returns the default, or safe null, for the specified type if no flags match the specified name.</returns>
        /// <exception cref="FormatException">The conversino method must understand the flag's value format.</exception>
        /// <exception cref="ArgumentNullException">The conversion method cannot be null.</exception>
        public static async Task<T> GetAsync<T>(String flagName, Func<String, Task<T>> asyncConversionMethod) {
            //Exceptions
            if (asyncConversionMethod == null)
                throw new ArgumentNullException(nameof(asyncConversionMethod));

            //Get
            var flagValue = await GetAsync(flagName);

            if (flagValue == null)
                return default(T);

            try {
                return await asyncConversionMethod(flagValue);
            }
            catch (FormatException ex) {
                throw new FormatException("The conversion method does not understand the flag's value format.", ex);
            }
        }

        /// <summary>
        /// Tries to get a flag's value as the specified type asynchronously from the flag's name using a specified conversion method.
        /// </summary>
        /// <typeparam name="T">The type of the flag's value.</typeparam>
        /// <param name="flagName">The flag's name</param>
        /// <param name="conversionMethod">The method used to convert the value from <see cref="String"/> to <see cref="T"/>.</param>
        /// <returns>Returns a <see cref="T"/> representing the flag's value. Returns the default, or safe null, for the specified type if no flags match the specified name.</returns>
        public static async Task<T> GetAsync<T>(String flagName, Func<String, T> conversionMethod)
            => await GetAsync<T>(flagName, flagValue => Task.Run(() => conversionMethod(flagValue)));

        public static async Task<T> GetAsync<T>(String flagName) where T : class
            => await GetAsync(flagName) as T;
        #endregion

        #region Set
        /// <summary>
        /// Sets a flag's value from the flag's name and a <see cref="String"/> value.
        /// </summary>
        /// <param name="flagName">The flag's name.</param>
        /// <param name="value">The flag's value to set, as <see cref="String"/>.</param>
        public static void Set(String flagName, String value)
            => SetAsync(flagName, value).RunSynchronously();

        /// <summary>
        /// Sets a flag's value asynchronously from the flag's name and a <see cref="String"/> value.
        /// </summary>
        /// <param name="flagName">The flag's name.</param>
        /// <param name="value">The flag's value to set, as <see cref="String"/>.</param>
        /// <exception cref="ArgumentNullException">The <see cref="value"/> cannot be <code>null</code>.</exception>
        /// <exception cref="ArgumentException">The flag's name cannot be <code>null</code> or empty.</exception>
        public static async Task SetAsync(String flagName, String value) {
            //Exceptions
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (String.IsNullOrEmpty(flagName))
                throw new ArgumentException("Argument is null or empty", nameof(flagName));

            //Set
            using (var db = new TContext()) {
                var flag = await db.Flags
                    .SingleOrDefaultAsync(f => f.Name.Equals(flagName));

                if (flag != null) {
                    //Update
                    flag.Value = value;
                    db.Entry(flag).State = EntityState.Modified;
                }
                else {
                    //Add
                    flag = new Flag(flagName, value);
                    db.Flags.Attach(flag);
                    db.Entry(flag).State = EntityState.Added;
                }
                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Sets a flag's value from the flag's name and it's value.
        /// </summary>
        /// <param name="flagName">The flag's name.</param>
        /// <param name="value">The flag's value to set.</param>
        /// <remarks>Calls <see cref="Object.ToString"/> on <see cref="value"/>.</remarks>
        public static void Set(String flagName, Object value)
            => Set(flagName, value.ToString());

        /// <summary>
        /// Sets a flag's value asynchronously from the flag's name and it's value.
        /// </summary>
        /// <param name="flagName">The flag's name.</param>
        /// <param name="value">The flag's value to set.</param>
        /// <remarks>Calls <see cref="Object.ToString"/> on <see cref="value"/>.</remarks>
        public static async Task SetAsync(String flagName, Object value)
            => await SetAsync(flagName, value.ToString());

        #endregion


    }
    public interface IFlagEntities : IDisposable {
        DbSet<Flag> Flags { get; set; }
    }
    public class Flag {
        public String Name { get; }
        public String Value { get; set; }

        public Flag(String name, String value)
        {
            this.Name = name;
            this.Value = value;
        }
    }
}