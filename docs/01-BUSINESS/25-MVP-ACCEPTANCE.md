# YumGo — MVP Acceptance Checklist

MVP được coi là hoàn thành khi một người có thể thực hiện trọn vẹn nghiệp vụ sau.

## Scenario A — Customer đặt COD

1. Register.
2. Login.
3. Chọn restaurant.
4. Chọn branch.
5. Xem menu.
6. Thêm món vào cart.
7. Chọn địa chỉ.
8. Checkout.
9. Chọn COD.
10. Place order.
11. Nhà hàng nhận order.
12. Nhà hàng accept.
13. Nhà hàng prepare.
14. Nhà hàng ready.
15. Driver nhận delivery.
16. Driver pickup.
17. Driver delivery.
18. Customer xem location.
19. Driver complete.
20. Order Delivered.
21. Customer review.

## Scenario B — Online payment

1. Customer checkout.
2. Chọn online payment.
3. Order/payment được tạo theo trạng thái chờ thanh toán.
4. Provider xác nhận payment.
5. Webhook được verify.
6. Payment thành công.
7. Order được xử lý tiếp.

## Scenario C — Restaurant reject

1. Customer tạo order.
2. Restaurant reject.
3. Customer nhận notification.
4. COD không cần refund tiền thực tế.
5. Online payment phải vào refund flow nếu tiền đã thu.

## Scenario D — Customer cancel

1. Customer tạo order.
2. Cancel trong khoảng cho phép.
3. Order chuyển Cancelled.
4. Nếu online payment đã thu, refund flow chạy.

## Scenario E — Duplicate request

1. Gửi Create Order lần 1.
2. Giả lập timeout client.
3. Gửi lại với cùng idempotency key.
4. Kết quả không được tạo 2 order.

## Scenario F — Concurrent driver acceptance

1. Có một delivery offer.
2. Hai driver cùng accept gần như đồng thời.
3. Chỉ một driver thắng.
4. Driver còn lại nhận conflict/no longer available.

## Scenario G — Security

- Customer A không đọc được order Customer B.
- Driver A không update delivery Driver B.
- RestaurantStaff A không update branch B.
- Frontend sửa role trong request cũng không có tác dụng.

## Scenario H — Resilience

- Worker restart không làm mất event.
- Payment webhook duplicate không tạo side effect duplicate.
- Notification provider down không làm mất order.
