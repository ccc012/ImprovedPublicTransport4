# Train Display - Updated integration

This folder contains the IPT-side bridge for Steam Workshop item `3233229958`.

## Scope

- Keeps the feature modular and isolated under `Integration/TrainDisplayUpdated`.
- Uses the game's selected-vehicle UI directly; it has no first-person-camera dependency.

## Behavior

When a supported public transport vehicle is selected, the integration shows a small overlay with:

- line name
- next destination
- current state

## Dependencies

- `Train Display - Updated` (`3233229958`) is the feature source.
- No additional camera mod is required.

## Credits

- Original upstream authors and translators as listed by the Workshop item.
- IPT integration code by the IPT fork maintainers.
