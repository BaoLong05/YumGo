export const hasPermission = (
  permissions: readonly string[],
  required: string,
): boolean => {
  return permissions.includes(required);
};

export const hasAnyPermission = (
  permissions: readonly string[],
  required: readonly string[],
): boolean => {
  return required.some((permission) =>
    permissions.includes(permission),
  );
};