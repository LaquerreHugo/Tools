using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using IqWareMigrateur.Models;

namespace IqWareMigrateur {
    /// <summary>
    /// Ensemble des fonctionnalité concernant les flags.
    /// </summary>
    public static class Flags
    {
        #region Get
        /// <summary>
        /// Obtien un flag.
        /// </summary>
        /// <param name="name">Le nom du flag a obtenir.</param>
        /// <returns>Retourne la valeur du flag recherche, retourne null si le flag n'existe pas.</returns>
        public static String GetFlag(String name) {
            using (var db = new MigrateurEntities()) {
                var flag = db.tblFlags
                    .SingleOrDefault(f => f.Name.Equals(name));

                return flag?.Value;
            }
        }

        /// <summary>
        /// Obtien un flag avec un type specifier.
        /// </summary>
        /// <typeparam name="T">Le type du flag a obtenir.</typeparam>
        /// <param name="name">Le nom du flag a obtenir.</param>
        /// <param name="conversionMethod">La methode de conversion pour convertir le flag a obtenir.</param>
        /// <returns>Retourne la valeur du flag recherche dans le type donne, retourne le safe null du type specifer si le flag n'existe pas.</returns>
        /// <exception cref="FormatException">La methode de conversion doit accepter le format du flag.</exception>
        public static T GetFlag<T>(String name, Func<String, T> conversionMethod) {
            var flag = GetFlag(name);
            if (flag == null)
                return default(T);
            try {
                return conversionMethod(flag);
            }
            catch (FormatException ex) {
                throw new FormatException("Le flag obtenu ne peu pas etre converti au type specifier avec la methode de conversion specifier.", ex);
            }
        }

        /// <summary>
        /// Obtien un flag avec un type specifier en faisant un safe cast.
        /// </summary>
        /// <typeparam name="T">Le type du flag a obtenir.</typeparam>
        /// <param name="name">Le nom du flag a obtenir.</param>
        /// <returns>Retourne la valeur du flag recherche dans le type donne, retourne le safe null du type specifer si le flag n'existe pas ou si il ne se convertie pas en <see cref="T"/> avec un cast.</returns>
        public static T GetFlag<T>(String name) where T : class
            => GetFlag(name) as T;
        #endregion

        #region Set
        /// <summary>
        /// Ajouter ou modifier un flag.
        /// </summary>
        /// <param name="name">Le nom du flag a ajouter ou modifier.</param>
        /// <param name="value">La valeur du flag a ajouter ou modfier.</param>
        /// <exception cref="ArgumentNullException">La value ne peu pas etre null.</exception>
        /// <exception cref="ArgumentException">Le name ne peu pas etre null, vide ou etre fait d'espaces.</exception>
        public static void AddOrUpdate(String name, String value) {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Argument is null or whitespace", nameof(name));

            using (var db = new MigrateurEntities()) {
                var flag = db.tblFlags
                    .SingleOrDefault(f => f.Name == name);

                if (flag != null) {
                    //Update
                    flag.Value = value;
                }
                else {
                    //Ajout
                    flag = new tblFlag
                    {
                        Name = name,
                        Value = value
                    };
                    db.tblFlags.Attach(flag);
                    db.Entry(flag).State = EntityState.Added;
                }
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Ajouter ou modifier un flag d'un type specifier en appelant <see cref="Object.ToString"/>.
        /// </summary>
        /// <param name="name">Le nom du flag a ajouter ou modifier.</param>
        /// <param name="value">La valeur du flag a ajouter ou modfier.</param>
        /// <exception cref="ArgumentNullException">La value ne peu pas etre null.</exception>
        /// <exception cref="ArgumentException">Le name ne peu pas etre null, vide ou etre fait d'espaces.</exception>
        public static void AddOrUpdate(String name, Object value)
            => AddOrUpdate(name, value.ToString());
        #endregion
    }
}