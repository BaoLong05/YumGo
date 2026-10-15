# YumGo — Business Specification v1.0

## 1. YumGo đang giải quyết bài toán gì?

YumGo là hệ thống cho phép:

**Khách chọn món → đặt hàng → thanh toán → nhà hàng chuẩn bị → tài xế giao → khách nhận hàng → đánh giá.**

Bên cạnh luồng khách hàng, hệ thống còn phải cho nhà hàng, tài xế, operations và admin thực hiện công việc tương ứng.

## 2. Actor và công việc

### Customer

Mục tiêu: mua đồ ăn.

Có thể:

- đăng ký/đăng nhập
- quản lý hồ sơ
- lưu địa chỉ
- tìm nhà hàng
- xem menu
- thêm món
- dùng khuyến mãi
- checkout
- đặt hàng
- thanh toán
- hủy đơn khi hợp lệ
- theo dõi delivery
- xem lịch sử
- đánh giá

### Restaurant Staff/Manager

Mục tiêu: bán món và xử lý đơn.

Có thể:

- mở/đóng branch
- quản lý menu
- bật/tắt món
- nhận order
- accept/reject
- prepare
- ready for pickup

### Driver

Mục tiêu: nhận delivery và giao hàng.

Có thể:

- online/offline
- nhận offer
- accept/reject
- pickup
- cập nhật vị trí
- hoàn tất delivery

### Operations

Mục tiêu: giữ hệ thống chạy ổn định.

Có thể:

- xem order
- xem delivery
- theo dõi driver
- xem payment
- can thiệp một số tình huống
- xem audit

### Admin

Mục tiêu: quản trị hệ thống.

Có thể:

- quản lý user
- quản lý role/permission
- quản lý restaurant
- cấu hình nền tảng theo quyền

## 3. Luồng nghiệp vụ lớn nhất

```text
Customer
   ↓
Browse restaurant
   ↓
Choose branch
   ↓
Choose menu item
   ↓
Cart
   ↓
Checkout
   ↓
Create Order
   ↓
Payment
   ↓
Restaurant accepts
   ↓
Preparing
   ↓
Ready for pickup
   ↓
Driver assignment
   ↓
Driver accepts
   ↓
Pickup
   ↓
Delivering
   ↓
Customer tracking
   ↓
Delivered
   ↓
Review
```

## 4. Những điều tuyệt đối không được phá vỡ

### 4.1 Không tin dữ liệu tiền từ client

Client chỉ gửi lựa chọn. Backend tự tính tiền.

### 4.2 Không tin trạng thái thanh toán từ client

Payment success phải do backend xác minh.

### 4.3 Không cho sửa trạng thái order tùy ý

Không có API kiểu:

```text
PATCH /orders/{id}
{ "status": "Delivered" }
```

rồi tin dữ liệu đó.

Phải có nghiệp vụ:

```text
AcceptOrder
StartPreparing
MarkReady
PickupOrder
StartDelivery
CompleteDelivery
```

### 4.4 Không cho đọc tài nguyên của người khác

Customer A chỉ đọc order của A.

### 4.5 Không tạo side effect hai lần

Critical operation phải idempotent.

## 5. Cách triển khai

Sau khi nghiệp vụ đã rõ, mỗi tính năng mới được chuyển thành:

```text
Business Feature
    ↓
Use Case
    ↓
Business Rules
    ↓
Domain Model
    ↓
Application Service/Handler
    ↓
Database / External Provider
    ↓
API
    ↓
Web / Mobile
    ↓
Tests
```

## 6. Thứ tự nên code

1. Identity.
2. Customer profile/address.
3. Restaurant/branch.
4. Menu.
5. Restaurant discovery.
6. Cart.
7. Promotion.
8. Checkout.
9. Order.
10. Restaurant order processing.
11. Payment.
12. Driver/assignment.
13. Delivery.
14. Realtime tracking.
15. Notifications/background jobs.
16. Review.
17. Operations/Admin.
18. Hardening/security/observability.

## 7. Tài liệu chi tiết

- `01-F01-REGISTER.md`
- `02-F02-LOGIN-SESSION.md`
- `03-F03-CUSTOMER-PROFILE.md`
- `04-F04-ADDRESSES.md`
- `05-F05-RESTAURANT-BRANCH.md`
- `06-F06-MENU.md`
- `07-F07-RESTAURANT-DISCOVERY.md`
- `08-F08-CART.md`
- `09-F09-PROMOTIONS.md`
- `10-F10-CHECKOUT.md`
- `11-F11-PLACE-ORDER.md`
- `12-F12-RESTAURANT-ORDER-PROCESSING.md`
- `13-F13-DRIVER-AVAILABILITY-ASSIGNMENT.md`
- `14-F14-DELIVERY.md`
- `15-F15-REALTIME-TRACKING.md`
- `16-F16-PAYMENT.md`
- `17-F17-CANCEL-REFUND.md`
- `18-F18-REVIEWS.md`
- `19-F19-NOTIFICATIONS.md`
- `20-F20-OPERATIONS-DASHBOARD.md`
- `21-F21-USER-ROLE-PERMISSION-ADMIN.md`
- `22-F22-AUDIT.md`
- `23-F23-EDGE-CASES.md`
- `24-BUSINESS-GLOSSARY.md`
- `25-MVP-ACCEPTANCE.md`

Đây là lớp **Business Specification**. Sau khi chốt lớp này mới dùng bộ technical docs cho API/database/architecture.
