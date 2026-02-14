using Application.DTOs;
using Application.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    public interface ICartService
    {
        // user methods

        /// <summary>
        /// Retrieves all cart items associated with the specified authenticated user.
        /// </summary>
        /// <param name="userId">The unique identifier of the authenticated user.</param>
        /// <returns>
        /// An ApiResponse containing a collection of CartDto representing the user's cart items.
        /// </returns>
        Task<ApiResponse<IEnumerable<CartDto>>> GetUserCart(int userId, int langId);


        /// <summary>
        /// Adds a product variant to the authenticated user's cart.
        /// If the variant already exists, the quantity is increased accordingly.
        /// </summary>
        /// <param name="userId">The unique identifier of the authenticated user.</param>
        /// <param name="variantId">The identifier of the product variant to add.</param>
        /// <param name="quantity">The quantity to add.</param>
        /// <returns>
        /// An ApiResponse containing the operation result message.
        /// </returns>
        Task<ApiResponse<string>> AddToUserCart(int userId, int variantId, int quantity);


        /// <summary>
        /// Increases the quantity of a specific product variant in the authenticated user's cart by one.
        /// </summary>
        /// <param name="userId">The unique identifier of the authenticated user.</param>
        /// <param name="variantId">The identifier of the product variant.</param>
        /// <returns>
        /// An ApiResponse containing the operation result message.
        /// </returns>
        Task<ApiResponse<string>> IncreaseUserAmount(int userId, int variantId);


        /// <summary>
        /// Decreases the quantity of a specific product variant in the authenticated user's cart by one.
        /// If the quantity reaches zero, the item may be removed based on business rules.
        /// </summary>
        /// <param name="userId">The unique identifier of the authenticated user.</param>
        /// <param name="variantId">The identifier of the product variant.</param>
        /// <returns>
        /// An ApiResponse containing the operation result message.
        /// </returns>
        Task<ApiResponse<string>> DecreaseUserAmount(int userId, int variantId);


        /// <summary>
        /// Removes a specific product variant from the authenticated user's cart.
        /// </summary>
        /// <param name="userId">The unique identifier of the authenticated user.</param>
        /// <param name="variantId">The identifier of the product variant to remove.</param>
        /// <returns>
        /// An ApiResponse containing the operation result message.
        /// </returns>
        Task<ApiResponse<string>> RemoveUserItem(int userId, int variantId);



        // guest methods

        /// <summary>
        /// Retrieves all cart items associated with the specified guest identifier,
        /// provided the cart has not expired.
        /// </summary>
        /// <param name="guestId">The unique identifier of the guest.</param>
        /// <returns>
        /// An ApiResponse containing a collection of CartDto representing the guest's cart items.
        /// </returns>
        Task<ApiResponse<IEnumerable<CartDto>>> GetGuestCart(string guestId, int langId);


        /// <summary>
        /// Adds a product variant to the guest cart.
        /// If the variant already exists, the quantity is increased accordingly.
        /// </summary>
        /// <param name="guestId">The unique identifier of the guest.</param>
        /// <param name="variantId">The identifier of the product variant to add.</param>
        /// <param name="quantity">The quantity to add.</param>
        /// <returns>
        /// An ApiResponse containing the operation result message.
        /// </returns>
        Task<ApiResponse<string>> AddToGuestCart(string guestId, int variantId, int quantity);


        /// <summary>
        /// Increases the quantity of a specific product variant in the guest cart by one.
        /// </summary>
        /// <param name="guestId">The unique identifier of the guest.</param>
        /// <param name="variantId">The identifier of the product variant.</param>
        /// <returns>
        /// An ApiResponse containing the operation result message.
        /// </returns>
        Task<ApiResponse<string>> IncreaseGuestAmount(string guestId, int variantId);


        /// <summary>
        /// Decreases the quantity of a specific product variant in the guest cart by one.
        /// If the quantity reaches zero, the item may be removed based on business rules.
        /// </summary>
        /// <param name="guestId">The unique identifier of the guest.</param>
        /// <param name="variantId">The identifier of the product variant.</param>
        /// <returns>
        /// An ApiResponse containing the operation result message.
        /// </returns>
        Task<ApiResponse<string>> DecreaseGuestAmount(string guestId, int variantId);


        /// <summary>
        /// Removes a specific product variant from the guest cart.
        /// </summary>
        /// <param name="guestId">The unique identifier of the guest.</param>
        /// <param name="variantId">The identifier of the product variant to remove.</param>
        /// <returns>
        /// An ApiResponse containing the operation result message.
        /// </returns>
        Task<ApiResponse<string>> RemoveGuestItem(string guestId, int variantId);


        /// <summary>
        /// Transfers all items from the guest cart to the authenticated user's cart
        /// after a successful login. Existing items are merged by increasing their quantities.
        /// The guest cart is cleared after the merge operation.
        /// </summary>
        /// <param name="guestId">The unique identifier of the guest cart to merge.</param>
        /// <returns>
        /// An ApiResponse containing the updated collection of CartDto for the authenticated user.
        /// </returns>
        Task<ApiResponse<IEnumerable<CartDto>>> Merge(int userId, string guestId);

    }
}
