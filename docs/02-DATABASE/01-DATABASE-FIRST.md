# Database First — YumGo PostgreSQL

## 1. Mục tiêu dữ liệu
YumGo là hệ thống đặt và giao đồ ăn. Database phải bảo vệ được:
- tính đúng của quan hệ giữa người dùng, nhà hàng, chi nhánh, menu, giỏ hàng, đơn hàng, thanh toán và giao hàng;
- tính toàn vẹn tiền tệ;
- lịch sử giao dịch;
- trạng thái quy trình;
- không cho phép một actor thao tác lên record không thuộc phạm vi của mình.

## 2. PostgreSQL conventions
- PK/FK: `varchar(21)` chứa NanoID.
- Tất cả tên bảng/cột dùng `snake_case`.
- `timestamptz` và lưu UTC.
- Tiền: `numeric(12,2)`.
- Rating: `smallint` với `check 1..5`.
- Latitude: `numeric(9,6)` trong `[-90,90]`.
- Longitude: `numeric(9,6)` trong `[-180,180]`.
- Boolean dùng `boolean not null` nếu không có trạng thái thứ ba.
- Status dùng `varchar` + `check constraint` để dễ mở rộng migration.
- Không dùng `serial`, `bigserial`, identity, UUID làm ID nghiệp vụ.

## 3. ID generation
NanoID được sinh ở application layer bằng alphabet URL-safe, độ dài 21. Mỗi bảng có unique primary key.

Pseudo-code:
```text
id = nanoid(21)
```

Khi insert, database vẫn bảo vệ uniqueness bằng PK; nếu collision cực hiếm xảy ra thì retry transaction.

## 4. Soft delete
Áp dụng `deleted_at` cho master data cần khôi phục/audit:
- users
- restaurants
- restaurant_branches
- menu_categories
- menu_items

Không soft-delete transaction đã phát sinh:
- orders
- order_items
- payments
- payment_transactions
- deliveries
- delivery_assignments
- refunds
- audit_logs

## 5. Snapshot rule
Đơn hàng phải snapshot các dữ liệu có thể thay đổi về sau:
- order delivery address;
- item name;
- item unit price;
- promotion code/value;
- restaurant/branch display name nếu cần audit.

Không đọc lại giá menu hiện tại để tái dựng lịch sử đơn hàng.
