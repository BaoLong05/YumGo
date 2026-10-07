# State Machines

## Order
```text
pending
 -> confirmed
 -> preparing
 -> ready
 -> picked_up
 -> delivering
 -> delivered

pending -> cancelled
confirmed -> cancelled
preparing -> cancelled (only when cancellation policy allows)
```

Forbidden examples:
- delivered -> preparing
- cancelled -> confirmed
- delivering -> pending

## Payment
```text
pending -> processing -> paid
pending -> failed
processing -> failed
paid -> refunded / partially_refunded
```

## Delivery
```text
pending -> assigned -> picked_up -> delivering -> delivered
pending -> cancelled
assigned -> failed
```

## Driver availability
```text
offline <-> online
online -> busy
busy -> online
```

A state transition must be performed by a dedicated command/use case, not by arbitrary property assignment from a controller.
