# Train Display - Updated integration

This folder contains the IPT-side bridge for Steam Workshop item `3233229958`.

## Scope

- Keeps the feature modular and isolated under `Integration/TrainDisplayUpdated`.
- Avoids any changes to `UI/`, `OptionsPanel`, `ModSetting`, or menu code.
- Uses a runtime fallback so IPT still loads if `First Person Camera - Continued` is absent.

## Behavior

When a supported transport vehicle is being followed, the integration shows a small overlay with:

- line name
- next destination
- current state

## Dependencies

- `Train Display - Updated` (`3233229958`) is the feature source.
- `First Person Camera - Continued` (`3236046692`) is optional but recommended for the intended first-person experience.

## Credits

- Original upstream authors and translators as listed by the Workshop item.
- IPT integration code by the IPT fork maintainers.
