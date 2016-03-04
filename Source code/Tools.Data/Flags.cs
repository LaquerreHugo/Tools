using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.Data.Entity;

namespace Tools.Data
{
    public class Flags<TContext, TEntity, TValue>
        where TContext : DbContext, Flags.IEntities<TValue>, new()
        where TEntity : Flags.IEntity<TValue>, new() {

        #region Get
        #region Sync
        /// <summary>
        /// Tries to get a flag's value as <see cref="String"/> from the flag's name.
        /// </summary>
        /// <param name="name">The flag's name.</param>
        /// <returns>Returns a <see cref="String"/> representing the flag's value. Returns <code>null</code> if no flags match the specified name.</returns>
        /// <exception cref="ArgumentException">The flag's name cannot be <code>null</code> or empty.</exception>
        public static TValue Get(String name)
            => GetAsync(name).Result;

        /// <summary>
        /// Tries to get a flag's value as the specified type from the flag's name using a specified conversion method.
        /// </summary>
        /// <typeparam name="T">The type of the flag's value.</typeparam>
        /// <param name="name">The flag's name</param>
        /// <param name="conversionMethod">The method used to convert the value from <see cref="String"/> to <see cref="T"/>.</param>
        /// <returns>Returns a <see cref="T"/> representing the flag's value. Returns the default, or safe null, for the specified type if no flags match the specified name.</returns>
        public static T Get<T>(String name, Func<TValue, T> conversionMethod)
            => GetAsync(name, conversionMethod).Result;

        /// <summary>
        /// Tries to get a flag's value as the specified type from the flag's name using a safe cast.
        /// </summary>
        /// <typeparam name="T">The type of the flag's value.</typeparam>
        /// <param name="name">The flag's name.</param>
        /// <returns>Returns a <see cref="T"/> representing the flag's value. Returns the default, or safe null, for the specified type if no flags match the specified name or if conversion does not work.</returns>
        public static T Get<T>(String name) where T : class
            => Get(name) as T;
        #endregion

        /// <summary>
        /// Tries to get a flag's value asynchronously from the flag's name.
        /// </summary>
        /// <param name="name">The flag's name.</param>
        /// <returns>Returns a <see cref="TValue"/> representing the flag's value. Returns <code>null</code> if no flags match the specified name.</returns>
        /// <exception cref="ArgumentException">The flag's name cannot be <code>null</code> or empty.</exception>
        public static async Task<TValue> GetAsync(String name) {
            //Exceptions
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("Argument is null or empty", nameof(name));

            //Get
            using (var db = new TContext()) {
                var flag = await db.Flags
                    .SingleOrDefaultAsync(f => f.Name.Equals(name));
                return flag == default(Flags.IEntity<TValue>) 
                    ? default(TValue) 
                    : flag.Value;
            }
        }

        /// <summary>
        /// Tries to get a flag's value as the specified type asynchronously from the flag's name using a specified asynchronous conversion method.
        /// </summary>
        /// <typeparam name="T">The type of the flag's value.</typeparam>
        /// <param name="name">The flag's name</param>
        /// <param name="asyncConversionMethod">The asynchronous method used to convert the value from <see cref="TValue"/> to <see cref="T"/>.</param>
        /// <returns>Returns a <see cref="T"/> representing the flag's value. Returns the default, or safe null, for the specified type if no flags match the specified name.</returns>
        /// <exception cref="FormatException">The conversino method must understand the flag's value format.</exception>
        /// <exception cref="ArgumentNullException">The conversion method cannot be null.</exception>
        public static async Task<T> GetAsync<T>(String name, Func<TValue, Task<T>> asyncConversionMethod) {
            //Exceptions
            if (asyncConversionMethod == null)
                throw new ArgumentNullException(nameof(asyncConversionMethod));

            //Get
            var flagValue = await GetAsync(name);

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
        /// <param name="name">The flag's name</param>
        /// <param name="conversionMethod">The method used to convert the value from <see cref="TValue"/> to <see cref="T"/>.</param>
        /// <returns>Returns a <see cref="T"/> representing the flag's value. Returns the default, or safe null, for the specified type if no flags match the specified name.</returns>
        public static async Task<T> GetAsync<T>(String name, Func<TValue, T> conversionMethod)
            => await GetAsync<T>(name, flagValue => Task.Run(() => conversionMethod(flagValue)));

        public static async Task<T> GetAsync<T>(String name) where T : class
            => await GetAsync(name) as T;
        #endregion

        #region Set
        /// <summary>
        /// Sets a flag's value asynchronously from the flag's name and a value.
        /// </summary>
        /// <param name="name">The flag's name.</param>
        /// <param name="value">The flag's value to set, as <see cref="TValue"/>.</param>
        /// <exception cref="ArgumentNullException">The <see cref="value"/> cannot be <code>null</code>.</exception>
        /// <exception cref="ArgumentException">The flag's name cannot be <code>null</code> or empty.</exception>
        public static async Task SetAsync(String name, TValue value) {
            //Exceptions
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("Argument is null or empty", nameof(name));

            //Set
            using (var context = new TContext()) {
                var flag = await context.Flags
                    .SingleOrDefaultAsync(f => f.Name.Equals(name));

                if (flag != null) {
                    //Update
                    flag.Value = value;
                    context.Entry(flag).State = EntityState.Modified;
                }
                else {
                    //Add
                    flag = new TEntity {
                        Name = name,
                        Value = value
                    };
                    context.Flags.Attach(flag);
                    context.Entry(flag).State = EntityState.Added;
                }
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Sets a flag's value from the flag's name and a <see cref="String"/> value.
        /// </summary>
        /// <param name="name">The flag's name.</param>
        /// <param name="value">The flag's value to set, as <see cref="TValue"/>.</param>
        public static void Set(String name, TValue value)
            => SetAsync(name, value).RunSynchronously();
        #endregion
    }

    /// <summary>
    /// Represent flags.
    /// </summary>
    public class Flags<TContext, TEntity> : Flags<TContext, TEntity, String>
        where TContext : DbContext, Flags.IEntities<String>, new()
        where TEntity : Flags.IEntity<String>, new() {

        #region Set
        /// <summary>
        /// Sets a flag's value from the flag's name and it's value.
        /// </summary>
        /// <param name="name">The flag's name.</param>
        /// <param name="value">The flag's value to set.</param>
        /// <remarks>Calls <see cref="Object.ToString"/> on <see cref="value"/>.</remarks>
        public static void Set(String name, Object value)
            => Flags<TContext, TEntity, String>.Set(name, value.ToString());

        /// <summary>
        /// Sets a flag's value asynchronously from the flag's name and it's value.
        /// </summary>
        /// <param name="name">The flag's name.</param>
        /// <param name="value">The flag's value to set.</param>
        /// <remarks>Calls <see cref="Object.ToString"/> on <see cref="value"/>.</remarks>
        public static async Task SetAsync(String name, Object value)
            => await Flags<TContext, TEntity, String>.SetAsync(name, value.ToString());
        #endregion

    }

    public class Flags<TContext> : Flags<TContext, Flags.DefaultEntity>
        where TContext : DbContext, Flags.IEntities<String>, new()
    {}

    public class Flags//: Flags<ToolsEntities, Flag>
    {
        public class DefaultEntity : Flags.IEntity<String> {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public interface IEntity<TValue> {
            [Key]
            String Name { get; set; }
            [Required]
            TValue Value { get; set; }
        }

        public interface IEntities<TValue> : IDisposable {
            DbSet<IEntity<TValue>> Flags { get; set; }
        }
    }

}
