# Razorpay Payment Integration Setup

This document explains how to set up and use the Razorpay payment gateway integration in the AuthenticationDemo project.

## Prerequisites

1. **Razorpay Account**: Sign up at [Razorpay Dashboard](https://dashboard.razorpay.com/)
2. **API Keys**: Get your Key ID and Key Secret from the Razorpay Dashboard
3. **Webhook URL**: Configure webhook endpoints for payment status updates

## Configuration

### 1. Update appsettings.json

Replace the placeholder values in `appsettings.json`:

```json
{
  "Razorpay": {
    "KeyId": "rzp_test_YOUR_KEY_ID",
    "KeySecret": "YOUR_KEY_SECRET"
  }
}
```

**Note**:

- Use `rzp_test_` prefix for test environment
- Use `rzp_live_` prefix for production environment

### 2. Database Migration

Run the following commands to create the Payment table:

```bash
dotnet ef migrations add AddPaymentEntity
dotnet ef database update
```

## API Endpoints

### Payment Creation

**POST** `/api/payment/create`

Creates a new payment for an order.

```json
{
  "orderId": "order_123",
  "amount": 1000.0,
  "currency": "INR",
  "description": "Payment for order #123",
  "receipt": "receipt_123",
  "email": "customer@example.com",
  "contact": "+919876543210"
}
```

### Payment Capture

**POST** `/api/payment/capture` (Admin only)

Captures a pending payment.

```json
{
  "paymentId": "pay_1234567890",
  "amount": 1000.0,
  "currency": "INR"
}
```

### Payment Refund

**POST** `/api/payment/refund` (Admin only)

Refunds a captured payment.

```json
{
  "paymentId": "pay_1234567890",
  "amount": 1000.0,
  "reason": "customer_requested",
  "speed": "normal"
}
```

### Payment Verification

**POST** `/api/payment/verify`

Verifies payment signature for security.

```json
{
  "razorpayPaymentId": "pay_1234567890",
  "razorpayOrderId": "order_123",
  "razorpaySignature": "signature_hash"
}
```

### Get User Payments

**GET** `/api/payment/user`

Returns all payments for the authenticated user.

### Get Order Payments

**GET** `/api/payment/order/{orderId}`

Returns all payments for a specific order.

### Get Payment by ID

**GET** `/api/payment/{id}`

Returns payment details by internal ID.

### Get Payment by Razorpay ID

**GET** `/api/payment/razorpay/{razorpayPaymentId}`

Returns payment details by Razorpay payment ID.

### Admin Endpoints

**GET** `/api/payment/all` (Admin only)
**GET** `/api/payment/status/{status}` (Admin only)

## Payment Flow

### 1. Create Payment

1. Client calls `/api/payment/create` with order details
2. System creates Razorpay order
3. Returns payment details with Razorpay order ID

### 2. Process Payment

1. Client redirects to Razorpay payment page using the order ID
2. User completes payment on Razorpay
3. Razorpay redirects back to your application

### 3. Capture Payment

1. Admin calls `/api/payment/capture` with payment ID
2. System captures the payment with Razorpay
3. Payment status updated to "captured"

### 4. Webhook Processing

1. Razorpay sends webhook to `/api/payment/webhook`
2. System verifies webhook signature
3. Updates payment status based on webhook event

## Payment Statuses

- **pending**: Payment created but not captured
- **captured**: Payment successfully captured
- **failed**: Payment failed
- **refunded**: Payment refunded

## Security Features

1. **Signature Verification**: All webhooks are verified using HMAC SHA256
2. **Role-based Access**: Admin-only endpoints for sensitive operations
3. **User Authorization**: Users can only access their own payments
4. **Amount Validation**: Prevents overcharging and undercharging

## Error Handling

The API returns appropriate HTTP status codes and error messages:

- `400 Bad Request`: Invalid request data
- `401 Unauthorized`: Missing or invalid authentication
- `403 Forbidden`: Insufficient permissions
- `404 Not Found`: Payment not found
- `500 Internal Server Error`: Server-side errors

## Testing

### Test Cards

Use these test card numbers for testing:

- **Success**: 4111 1111 1111 1111
- **Failure**: 4000 0000 0000 0002
- **Insufficient Funds**: 4000 0000 0000 9995

### Test UPI

- **Success**: success@razorpay
- **Failure**: failure@razorpay

## Production Considerations

1. **HTTPS**: Always use HTTPS in production
2. **Webhook Security**: Verify webhook signatures
3. **Error Logging**: Implement proper error logging
4. **Monitoring**: Monitor payment success/failure rates
5. **Compliance**: Ensure PCI DSS compliance if handling card data

## Support

For Razorpay-specific issues, refer to:

- [Razorpay Documentation](https://razorpay.com/docs/)
- [Razorpay Support](https://razorpay.com/support/)

For application-specific issues, check the application logs and error responses.
